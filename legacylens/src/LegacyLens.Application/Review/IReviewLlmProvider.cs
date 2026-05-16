namespace LegacyLens.Application.Review;

public interface IReviewLlmProvider
{
    string Name { get; }

    Task<string?> GenerateReviewAsync(
        LlmReviewPrompt prompt,
        CancellationToken cancellationToken);
}
