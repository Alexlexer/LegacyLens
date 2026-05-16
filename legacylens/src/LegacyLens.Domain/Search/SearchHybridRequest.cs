namespace LegacyLens.Domain.Search;

public sealed record SearchHybridRequest(
    string Query,
    string? RepoPath,
    int Limit = 10);
