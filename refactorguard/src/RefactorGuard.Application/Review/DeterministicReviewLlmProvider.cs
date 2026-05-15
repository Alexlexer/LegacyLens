namespace RefactorGuard.Application.Review;

public sealed class DeterministicReviewLlmProvider : IReviewLlmProvider
{
    public string Name => "Deterministic";

    public Task<string?> GenerateReviewAsync(
        LlmReviewPrompt prompt,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(null);
    }
}
