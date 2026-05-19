using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using LegacyLens.Application.DotNetAnalysis;

namespace LegacyLens.Infrastructure.DotNetAnalysis;

public sealed class RoslynSymbolScanner(
    IRoslynWorkspaceCache workspaceCache) : IRoslynSymbolScanner
{
    public async Task<DotNetWorkspaceScanResponse> ScanAsync(
        string repoRoot,
        CancellationToken cancellationToken)
    {
        using var lease = await workspaceCache.GetOrLoadAsync(repoRoot, cancellationToken);
        var loaded = lease.Workspace;
        var symbols = loaded.Result.Success
            ? await ScanProjectsAsync(loaded.Projects, cancellationToken)
            : ScanCSharpFilesSyntaxOnly(repoRoot, cancellationToken);

        var documentCount = loaded.Result.Success
            ? loaded.Result.DocumentCount
            : symbols.Select(symbol => symbol.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).Count();

        var warnings = lease.DiscoveryResult.Warnings.Concat(loaded.Result.Warnings).Distinct().ToList();
        if (!loaded.Result.Success && symbols.Count > 0)
        {
            warnings.Add("Roslyn MSBuild workspace loading failed. LegacyLens used a syntax-only C# fallback for symbol counts; references and compiler-aware facts remain unavailable.");
        }

        return new DotNetWorkspaceScanResponse(
            lease.DiscoveryResult.Selected,
            warnings,
            loaded.Result.ProjectCount,
            documentCount,
            symbols.Count,
            symbols.Take(50).ToList(),
            loaded.Result)
        {
            SymbolKindCounts = symbols
                .GroupBy(symbol => symbol.Kind, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase)
        };
    }

    private static async Task<IReadOnlyList<DotNetSymbolInfo>> ScanProjectsAsync(
        IReadOnlyList<Project> projects,
        CancellationToken cancellationToken)
    {
        var symbols = new List<DotNetSymbolInfo>();
        foreach (var project in projects)
        {
            foreach (var document in project.Documents.Where(d => d.SourceCodeKind == SourceCodeKind.Regular))
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken);
                if (root is null)
                    continue;

                symbols.AddRange(ScanDocument(project.Name, document.FilePath ?? document.Name, root));
            }
        }

        return symbols;
    }

    private static IReadOnlyList<DotNetSymbolInfo> ScanCSharpFilesSyntaxOnly(
        string repoRoot,
        CancellationToken cancellationToken)
    {
        var symbols = new List<DotNetSymbolInfo>();
        if (!Directory.Exists(repoRoot))
            return symbols;

        foreach (var filePath in Directory.EnumerateFiles(repoRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsExcludedPath(repoRoot, path)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var text = File.ReadAllText(filePath);
                var tree = CSharpSyntaxTree.ParseText(text, path: filePath, cancellationToken: cancellationToken);
                var root = tree.GetRoot(cancellationToken);
                symbols.AddRange(ScanDocument("syntax-only", filePath, root));
            }
            catch (IOException)
            {
                // Keep fallback best-effort. A locked/generated file should not
                // prevent the audit from producing the rest of the static facts.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return symbols;
    }

    private static bool IsExcludedPath(string repoRoot, string filePath)
    {
        var relative = Path.GetRelativePath(repoRoot, filePath);
        var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Any(part =>
            part.Equals("bin", StringComparison.OrdinalIgnoreCase)
            || part.Equals("obj", StringComparison.OrdinalIgnoreCase)
            || part.Equals(".git", StringComparison.OrdinalIgnoreCase)
            || part.Equals("node_modules", StringComparison.OrdinalIgnoreCase)
            || part.Equals("packages", StringComparison.OrdinalIgnoreCase)
            || part.Equals(".vs", StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<DotNetSymbolInfo> ScanDocument(
        string projectName,
        string filePath,
        SyntaxNode root)
    {
        foreach (var node in root.DescendantNodes().Where(IsSymbolNode))
        {
            var name = GetName(node);
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var lineSpan = node.SyntaxTree.GetLineSpan(node.Span);
            yield return new DotNetSymbolInfo(
                name,
                BuildFullName(node, name),
                GetKind(node),
                filePath,
                lineSpan.StartLinePosition.Line + 1,
                lineSpan.StartLinePosition.Character + 1,
                projectName);
        }
    }

    private static bool IsSymbolNode(SyntaxNode node)
    {
        return node is NamespaceDeclarationSyntax
            or FileScopedNamespaceDeclarationSyntax
            or ClassDeclarationSyntax
            or InterfaceDeclarationSyntax
            or RecordDeclarationSyntax
            or StructDeclarationSyntax
            or EnumDeclarationSyntax
            or MethodDeclarationSyntax
            or PropertyDeclarationSyntax;
    }

    private static string GetName(SyntaxNode node)
    {
        return node switch
        {
            NamespaceDeclarationSyntax n => n.Name.ToString(),
            FileScopedNamespaceDeclarationSyntax n => n.Name.ToString(),
            BaseTypeDeclarationSyntax t => t.Identifier.ValueText,
            MethodDeclarationSyntax m => m.Identifier.ValueText,
            PropertyDeclarationSyntax p => p.Identifier.ValueText,
            _ => string.Empty
        };
    }

    private static string GetKind(SyntaxNode node)
    {
        return node switch
        {
            NamespaceDeclarationSyntax or FileScopedNamespaceDeclarationSyntax => "namespace",
            ClassDeclarationSyntax => "class",
            InterfaceDeclarationSyntax => "interface",
            RecordDeclarationSyntax r when r.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) => "record struct",
            RecordDeclarationSyntax => "record",
            StructDeclarationSyntax => "struct",
            EnumDeclarationSyntax => "enum",
            MethodDeclarationSyntax => "method",
            PropertyDeclarationSyntax => "property",
            _ => "unknown"
        };
    }

    private static string BuildFullName(SyntaxNode node, string name)
    {
        var names = new Stack<string>();
        names.Push(name);
        for (var current = node.Parent; current is not null; current = current.Parent)
        {
            switch (current)
            {
                case BaseTypeDeclarationSyntax type:
                    names.Push(type.Identifier.ValueText);
                    break;
                case NamespaceDeclarationSyntax ns:
                    names.Push(ns.Name.ToString());
                    break;
                case FileScopedNamespaceDeclarationSyntax ns:
                    names.Push(ns.Name.ToString());
                    break;
            }
        }

        return string.Join(".", names);
    }
}
