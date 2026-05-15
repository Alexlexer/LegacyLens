namespace RefactorGuard.Application.DotNetAnalysis;

public sealed record DotNetAnalysisPreset(
    string Id,
    string Title,
    string Query,
    string Rationale);
