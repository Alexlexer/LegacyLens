using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RefactorGuard.Application.DotNetAnalysis;

namespace RefactorGuard.Infrastructure.DotNetAnalysis;

public sealed class RoslynSymbolScanner(
    IDotNetWorkspaceDiscovery discovery,
    RoslynWorkspaceLoader workspaceLoader) : IRoslynSymbolScanner
{
    public async Task<DotNetWorkspaceScanResponse> ScanAsync(
        string repoRoot,
        CancellationToken cancellationToken)
    {
        var discoveryResult = await discovery.DiscoverAsync(repoRoot, cancellationToken);
        var loaded = await workspaceLoader.LoadWorkspaceForScanAsync(discoveryResult, cancellationToken);
        using (loaded)
        {
            var symbols = loaded.Result.Success
                ? await ScanProjectsAsync(loaded.Projects, cancellationToken)
                : [];

            return new DotNetWorkspaceScanResponse(
                discoveryResult.Selected,
                discoveryResult.Warnings.Concat(loaded.Result.Warnings).Distinct().ToList(),
                loaded.Result.ProjectCount,
                loaded.Result.DocumentCount,
                symbols.Count,
                symbols.Take(50).ToList(),
                loaded.Result);
        }
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
