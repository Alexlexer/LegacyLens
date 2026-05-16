using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using LegacyLens.Application.DotNetAnalysis;

namespace LegacyLens.Infrastructure.DotNetAnalysis;

public sealed class RoslynReferenceAnalyzer(
    IDotNetWorkspaceDiscovery discovery,
    RoslynWorkspaceLoader workspaceLoader) : IRoslynReferenceAnalyzer
{
    private const int MaxMatchedSymbols = 10;
    private const int MaxReferencesPerSymbol = 100;
    private const int MaxTotalReferences = 500;

    public async Task<RoslynReferenceAnalysisResult> FindReferencesAsync(
        RoslynReferenceAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RepoPath))
            throw new ArgumentException("Repository path is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.SymbolName))
            throw new ArgumentException("Symbol name is required.", nameof(request));

        var discoveryResult = await discovery.DiscoverAsync(request.RepoPath, cancellationToken);
        var loaded = await workspaceLoader.LoadWorkspaceForScanAsync(discoveryResult, cancellationToken);
        using (loaded)
        {
            if (!loaded.Result.Success || loaded.Solution is null)
            {
                return new RoslynReferenceAnalysisResult(
                    false,
                    loaded.Result.WorkspacePath,
                    loaded.Result.WorkspaceKind,
                    request.SymbolName,
                    [],
                    [],
                    loaded.Result.Warnings,
                    loaded.Result.ErrorMessage);
            }

            var warnings = loaded.Result.Warnings.Concat(loaded.Result.Diagnostics).Distinct().ToList();
            var matched = await FindMatchingSymbolsAsync(loaded.Projects, request, cancellationToken);
            if (matched.Count > 1)
            {
                warnings.Add("Multiple symbols matched. Results may include references for more than one symbol.");
            }

            var effectiveMaxTotal = Math.Clamp(request.MaxResults ?? MaxTotalReferences, 1, MaxTotalReferences);
            var references = new List<RoslynReferenceInfo>();
            foreach (var match in matched.Take(MaxMatchedSymbols))
            {
                references.AddRange(GetDefinitionReferences(match));
                if (references.Count >= effectiveMaxTotal)
                    break;

                var found = await SymbolFinder.FindReferencesAsync(match.Symbol, loaded.Solution, cancellationToken);
                foreach (var referencedSymbol in found)
                {
                    foreach (var location in referencedSymbol.Locations
                        .Where(l => l.Location.IsInSource)
                        .Take(MaxReferencesPerSymbol))
                    {
                        if (references.Count >= effectiveMaxTotal)
                            break;

                        var reference = ToReference(match, location.Location, "Reference", isDefinition: false);
                        if (reference is not null)
                            references.Add(reference);
                    }
                }
            }

            var matchedSymbols = matched
                .Take(MaxMatchedSymbols)
                .Select(m => m.Info)
                .ToList();

            return new RoslynReferenceAnalysisResult(
                true,
                loaded.Result.WorkspacePath,
                loaded.Result.WorkspaceKind,
                request.SymbolName,
                matchedSymbols,
                references.Take(effectiveMaxTotal).ToList(),
                warnings,
                null);
        }
    }

    private static async Task<IReadOnlyList<SymbolMatch>> FindMatchingSymbolsAsync(
        IReadOnlyList<Project> projects,
        RoslynReferenceAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        var matches = new List<SymbolMatch>();
        foreach (var project in projects)
        {
            foreach (var document in project.Documents.Where(d => d.SourceCodeKind == SourceCodeKind.Regular))
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken);
                var model = await document.GetSemanticModelAsync(cancellationToken);
                if (root is null || model is null)
                    continue;

                foreach (var node in root.DescendantNodes().Where(IsSymbolNode))
                {
                    var symbol = model.GetDeclaredSymbol(node, cancellationToken);
                    if (symbol is null)
                        continue;

                    var info = ToMatchedSymbol(symbol, project.Name);
                    if (info is null || !Matches(info, request))
                        continue;

                    matches.Add(new SymbolMatch(symbol, info, LocationDistance(info, request)));
                }
            }
        }

        return matches
            .OrderBy(m => m.Distance)
            .ThenBy(m => m.Info.FullName, StringComparer.OrdinalIgnoreCase)
            .Take(MaxMatchedSymbols)
            .ToList();
    }

    private static bool Matches(RoslynMatchedSymbol symbol, RoslynReferenceAnalysisRequest request)
    {
        var nameMatches = string.Equals(symbol.Name, request.SymbolName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(symbol.FullName, request.SymbolName, StringComparison.OrdinalIgnoreCase);
        var kindMatches = string.IsNullOrWhiteSpace(request.SymbolKind)
            || string.Equals(symbol.Kind, request.SymbolKind, StringComparison.OrdinalIgnoreCase);
        return nameMatches && kindMatches;
    }

    private static int LocationDistance(RoslynMatchedSymbol symbol, RoslynReferenceAnalysisRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FilePath))
            return 0;

        var sameFile = string.Equals(
            Path.GetFullPath(symbol.FilePath),
            Path.GetFullPath(request.FilePath),
            StringComparison.OrdinalIgnoreCase);
        if (!sameFile)
            return int.MaxValue / 2;

        var lineDistance = request.Line.HasValue ? Math.Abs(symbol.Line - request.Line.Value) : 0;
        var columnDistance = request.Column.HasValue ? Math.Abs(symbol.Column - request.Column.Value) : 0;
        return lineDistance * 1000 + columnDistance;
    }

    private static IEnumerable<RoslynReferenceInfo> GetDefinitionReferences(SymbolMatch match)
    {
        foreach (var location in match.Symbol.Locations.Where(l => l.IsInSource))
        {
            var reference = ToReference(match, location, "Definition", isDefinition: true);
            if (reference is not null)
                yield return reference;
        }
    }

    private static RoslynMatchedSymbol? ToMatchedSymbol(ISymbol symbol, string projectName)
    {
        var location = symbol.Locations.FirstOrDefault(l => l.IsInSource);
        if (location is null)
            return null;

        var span = location.GetLineSpan();
        return new RoslynMatchedSymbol(
            symbol.Name,
            symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            ToKind(symbol),
            location.SourceTree?.FilePath ?? string.Empty,
            span.StartLinePosition.Line + 1,
            span.StartLinePosition.Character + 1,
            projectName);
    }

    private static RoslynReferenceInfo? ToReference(
        SymbolMatch match,
        Location location,
        string referenceKind,
        bool isDefinition)
    {
        if (!location.IsInSource)
            return null;

        var span = location.GetLineSpan();
        if (string.IsNullOrWhiteSpace(span.Path))
            return null;

        return new RoslynReferenceInfo(
            match.Info.Name,
            match.Info.FullName,
            match.Info.Kind,
            span.Path,
            span.StartLinePosition.Line + 1,
            span.StartLinePosition.Character + 1,
            match.Info.ProjectName,
            match.Symbol.ContainingSymbol?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            referenceKind,
            isDefinition);
    }

    private static bool IsSymbolNode(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax
            or InterfaceDeclarationSyntax
            or RecordDeclarationSyntax
            or StructDeclarationSyntax
            or EnumDeclarationSyntax
            or MethodDeclarationSyntax
            or PropertyDeclarationSyntax;
    }

    private static string ToKind(ISymbol symbol)
    {
        return symbol switch
        {
            INamedTypeSymbol { TypeKind: TypeKind.Class, IsRecord: true } => "record",
            INamedTypeSymbol { TypeKind: TypeKind.Struct, IsRecord: true } => "record struct",
            INamedTypeSymbol { TypeKind: TypeKind.Class } => "class",
            INamedTypeSymbol { TypeKind: TypeKind.Interface } => "interface",
            INamedTypeSymbol { TypeKind: TypeKind.Struct } => "struct",
            INamedTypeSymbol { TypeKind: TypeKind.Enum } => "enum",
            IMethodSymbol => "method",
            IPropertySymbol => "property",
            _ => symbol.Kind.ToString()
        };
    }

    private sealed record SymbolMatch(
        ISymbol Symbol,
        RoslynMatchedSymbol Info,
        int Distance);
}
