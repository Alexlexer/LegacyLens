using System.ComponentModel.DataAnnotations;

namespace LegacyLens.Application.Review;

public sealed class ReviewEnrichmentOptions
{
    public const string SectionName = "RefactorGuard:ReviewEnrichment";

    [Range(1, 100)]
    public int MaxFilesToEnrich { get; init; } = 10;

    [Range(1, 50)]
    public int MaxSearchResultsPerFile { get; init; } = 5;

    [Range(1, 50_000)]
    public int MaxSkeletonLength { get; init; } = 4000;

    [Range(1, 50_000)]
    public int MaxBlockLength { get; init; } = 4000;

    [Range(1, 10_000)]
    public int MaxRelatedResultSnippetLength { get; init; } = 1000;

    [Range(1, 50)]
    public int MaxSymbolsForReferenceAnalysis { get; init; } = 10;

    [Range(1, 500)]
    public int MaxReferencesPerSymbol { get; init; } = 50;

    [Range(1, 2000)]
    public int MaxTotalRoslynReferences { get; init; } = 200;
}
