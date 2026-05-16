using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RefactorGuard.Application.Review;
using RefactorGuard.Infrastructure;

namespace RefactorGuard.Infrastructure.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddRefactorGuardInfrastructure_ReturnsServices()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var result = services.AddRefactorGuardInfrastructure(configuration);

        Assert.Same(services, result);
    }

    [Fact]
    public void AddRefactorGuardInfrastructure_BindsReviewEnrichmentOptions()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LegacyLens:ReviewEnrichment:MaxFilesToEnrich"] = "7",
                ["LegacyLens:ReviewEnrichment:MaxSearchResultsPerFile"] = "4",
                ["LegacyLens:ReviewEnrichment:MaxSkeletonLength"] = "3000",
                ["LegacyLens:ReviewEnrichment:MaxBlockLength"] = "3500",
                ["LegacyLens:ReviewEnrichment:MaxRelatedResultSnippetLength"] = "900"
            })
            .Build();

        services.AddRefactorGuardInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<ReviewEnrichmentOptions>();

        Assert.Equal(7, options.MaxFilesToEnrich);
        Assert.Equal(4, options.MaxSearchResultsPerFile);
        Assert.Equal(3000, options.MaxSkeletonLength);
        Assert.Equal(3500, options.MaxBlockLength);
        Assert.Equal(900, options.MaxRelatedResultSnippetLength);
    }
}
