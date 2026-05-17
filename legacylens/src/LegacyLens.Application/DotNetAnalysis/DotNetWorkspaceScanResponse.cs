namespace LegacyLens.Application.DotNetAnalysis;

public sealed record DotNetWorkspaceScanResponse(
    DotNetWorkspaceCandidate? SelectedWorkspace,
    IReadOnlyList<string> Warnings,
    int ProjectCount,
    int DocumentCount,
    int SymbolCount,
    IReadOnlyList<DotNetSymbolInfo> Symbols,
    RoslynWorkspaceLoadResult LoadResult)
{
    public IReadOnlyDictionary<string, int> SymbolKindCounts { get; init; } =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
}
