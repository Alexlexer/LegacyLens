namespace RefactorGuard.Application.DotNetAnalysis;

public sealed record DotNetAnalysisRequest(
    string? RepoPath = null,
    IReadOnlyList<string>? Presets = null,
    int LimitPerPreset = 10);
