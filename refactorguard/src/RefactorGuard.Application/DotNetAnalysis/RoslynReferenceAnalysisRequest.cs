namespace RefactorGuard.Application.DotNetAnalysis;

public sealed record RoslynReferenceAnalysisRequest(
    string? RepoPath,
    string SymbolName,
    string? SymbolKind = null,
    string? FilePath = null,
    int? Line = null,
    int? Column = null,
    int? MaxResults = null);
