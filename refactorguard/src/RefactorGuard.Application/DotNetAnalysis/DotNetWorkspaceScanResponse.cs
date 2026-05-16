namespace RefactorGuard.Application.DotNetAnalysis;

public sealed record DotNetWorkspaceScanResponse(
    DotNetWorkspaceCandidate? SelectedWorkspace,
    IReadOnlyList<string> Warnings,
    int ProjectCount,
    int DocumentCount,
    int SymbolCount,
    IReadOnlyList<DotNetSymbolInfo> Symbols,
    RoslynWorkspaceLoadResult LoadResult);
