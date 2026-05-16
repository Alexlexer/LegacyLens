namespace RefactorGuard.Application.DotNetAnalysis;

public sealed record RoslynReferenceAnalysisResult(
    bool Success,
    string? WorkspacePath,
    DotNetWorkspaceKind? WorkspaceKind,
    string QuerySymbolName,
    IReadOnlyList<RoslynMatchedSymbol> MatchedSymbols,
    IReadOnlyList<RoslynReferenceInfo> References,
    IReadOnlyList<string> Warnings,
    string? ErrorMessage);
