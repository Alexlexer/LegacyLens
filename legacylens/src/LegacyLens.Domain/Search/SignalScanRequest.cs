namespace LegacyLens.Domain.Search;

public sealed record SignalScanRequest(
    string RepoPath,
    IReadOnlyList<string>? Categories = null,
    int? TopKPerSignal = null,
    bool IncludeSnippets = true,
    string? ContextMode = null);
