namespace RefactorGuard.Domain.Search;

public sealed record SearchResultsResponse(IReadOnlyList<GpuSearchResult> Results);
