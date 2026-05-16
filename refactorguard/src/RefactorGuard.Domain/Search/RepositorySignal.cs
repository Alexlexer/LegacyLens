namespace RefactorGuard.Domain.Search;

public sealed record RepositorySignal(
    string Id,
    string Category,
    string Label,
    string? Description,
    string? Confidence,
    string? Query,
    IReadOnlyList<SignalMatch> Matches);
