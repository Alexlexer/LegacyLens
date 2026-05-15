namespace RefactorGuard.Domain.Search;

public sealed record ReadSkeletonResponse(
    string Result,
    string File,
    string? AbsoluteFile,
    string? Content,
    IReadOnlyList<int>? MatchLines,
    string? Language);
