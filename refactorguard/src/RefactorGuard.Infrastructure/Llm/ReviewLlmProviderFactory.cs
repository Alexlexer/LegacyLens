using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RefactorGuard.Application.Review;

namespace RefactorGuard.Infrastructure.Llm;

public sealed class ReviewLlmProviderFactory(
    IServiceProvider serviceProvider,
    IOptions<LlmProviderOptions> options) : IReviewLlmProvider
{
    public string Name => Current.Name;

    public Task<string?> GenerateReviewAsync(
        LlmReviewPrompt prompt,
        CancellationToken cancellationToken)
    {
        return Current.GenerateReviewAsync(prompt, cancellationToken);
    }

    private IReviewLlmProvider Current
    {
        get
        {
            return options.Value.Provider.ToLowerInvariant() switch
            {
                "deterministic" => serviceProvider.GetRequiredKeyedService<IReviewLlmProvider>("deterministic"),
                "lmstudio" or "lm-studio" => serviceProvider.GetRequiredKeyedService<IReviewLlmProvider>("lmstudio"),
                _ => throw new InvalidOperationException($"Unsupported LLM provider '{options.Value.Provider}'.")
            };
        }
    }
}
