using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RefactorGuard.Application.Review;
using RefactorGuard.Infrastructure.Llm;

namespace RefactorGuard.Infrastructure.Tests.Llm;

public sealed class ReviewLlmProviderFactoryTests
{
    [Fact]
    public void Name_ReturnsDeterministicProvider_WhenConfigured()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IReviewLlmProvider, DeterministicReviewLlmProvider>("deterministic");
        using var provider = services.BuildServiceProvider();
        var factory = new ReviewLlmProviderFactory(
            provider,
            Options.Create(new LlmProviderOptions { Provider = "Deterministic" }));

        Assert.Equal("Deterministic", factory.Name);
    }

    [Fact]
    public void Name_ReturnsDeterministicProvider_ByDefault()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IReviewLlmProvider, DeterministicReviewLlmProvider>("deterministic");
        using var provider = services.BuildServiceProvider();
        var factory = new ReviewLlmProviderFactory(
            provider,
            Options.Create(new LlmProviderOptions()));

        Assert.Equal("Deterministic", factory.Name);
    }

    [Fact]
    public void Name_ReturnsOllamaProvider_WhenConfigured()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IReviewLlmProvider>("ollama", new StubReviewLlmProvider("Ollama"));
        using var provider = services.BuildServiceProvider();
        var factory = new ReviewLlmProviderFactory(
            provider,
            Options.Create(new LlmProviderOptions { Provider = "Ollama" }));

        Assert.Equal("Ollama", factory.Name);
    }

    [Fact]
    public void Name_MatchesOllamaProvider_CaseInsensitively()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IReviewLlmProvider>("ollama", new StubReviewLlmProvider("Ollama"));
        using var provider = services.BuildServiceProvider();
        var factory = new ReviewLlmProviderFactory(
            provider,
            Options.Create(new LlmProviderOptions { Provider = "oLlAmA" }));

        Assert.Equal("Ollama", factory.Name);
    }

    [Fact]
    public void Name_ReturnsLmStudioProvider_WhenConfigured()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IReviewLlmProvider>("lmstudio", new StubReviewLlmProvider("LmStudio"));
        using var provider = services.BuildServiceProvider();
        var factory = new ReviewLlmProviderFactory(
            provider,
            Options.Create(new LlmProviderOptions { Provider = "LmStudio" }));

        Assert.Equal("LmStudio", factory.Name);
    }

    [Fact]
    public async Task GenerateReviewAsync_ThrowsForUnsupportedProvider()
    {
        var services = new ServiceCollection();
        using var provider = services.BuildServiceProvider();
        var factory = new ReviewLlmProviderFactory(
            provider,
            Options.Create(new LlmProviderOptions { Provider = "Unknown" }));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            factory.GenerateReviewAsync(new LlmReviewPrompt("repo", [], "diff"), CancellationToken.None));
    }

    private sealed class StubReviewLlmProvider(string name) : IReviewLlmProvider
    {
        public string Name { get; } = name;

        public Task<string?> GenerateReviewAsync(
            LlmReviewPrompt prompt,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<string?>("summary");
        }
    }
}
