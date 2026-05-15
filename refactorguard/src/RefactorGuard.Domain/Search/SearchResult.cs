namespace RefactorGuard.Domain.Search;

public sealed record SearchResult(
    string FilePath,
    int? Line,
    string Snippet,
    double? Score);
