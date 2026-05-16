using System.ComponentModel.DataAnnotations;
using RefactorGuard.Application.Review;

namespace RefactorGuard.Application.Tests;

public sealed class ReviewEnrichmentOptionsTests
{
    [Fact]
    public void Defaults_PreservePreviousEnrichmentLimits()
    {
        var options = new ReviewEnrichmentOptions();

        Assert.Equal(10, options.MaxFilesToEnrich);
        Assert.Equal(5, options.MaxSearchResultsPerFile);
        Assert.Equal(4000, options.MaxSkeletonLength);
        Assert.Equal(4000, options.MaxBlockLength);
        Assert.Equal(1000, options.MaxRelatedResultSnippetLength);
    }

    [Fact]
    public void Validation_RejectsNonPositiveValues()
    {
        var options = new ReviewEnrichmentOptions
        {
            MaxFilesToEnrich = 0,
            MaxSearchResultsPerFile = 0,
            MaxSkeletonLength = 0,
            MaxBlockLength = 0,
            MaxRelatedResultSnippetLength = 0
        };

        var results = Validate(options);

        Assert.Equal(5, results.Count);
    }

    [Fact]
    public void Validation_RejectsValuesAboveUpperBounds()
    {
        var options = new ReviewEnrichmentOptions
        {
            MaxFilesToEnrich = 101,
            MaxSearchResultsPerFile = 51,
            MaxSkeletonLength = 50_001,
            MaxBlockLength = 50_001,
            MaxRelatedResultSnippetLength = 10_001
        };

        var results = Validate(options);

        Assert.Equal(5, results.Count);
    }

    private static List<ValidationResult> Validate(ReviewEnrichmentOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            options,
            new ValidationContext(options),
            results,
            validateAllProperties: true);
        return results;
    }
}
