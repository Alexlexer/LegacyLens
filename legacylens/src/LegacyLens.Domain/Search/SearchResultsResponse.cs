namespace LegacyLens.Domain.Search;

public sealed record SearchResultsResponse(IReadOnlyList<GpuSearchResult> Results);
