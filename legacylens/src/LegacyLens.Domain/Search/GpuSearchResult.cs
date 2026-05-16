namespace LegacyLens.Domain.Search;

public sealed record GpuSearchResult(
    string File,
    string? AbsoluteFile,
    int? LineStart,
    int? LineEnd,
    double? Score,
    string? Reason,
    string? Snippet,
    string? Engine);
