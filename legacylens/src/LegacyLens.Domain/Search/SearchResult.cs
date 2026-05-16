namespace LegacyLens.Domain.Search;

public sealed record SearchResult(
    string FilePath,
    int? Line,
    string Snippet,
    double? Score);
