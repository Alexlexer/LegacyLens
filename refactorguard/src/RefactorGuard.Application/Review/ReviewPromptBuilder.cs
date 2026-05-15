using RefactorGuard.Domain.Git;

namespace RefactorGuard.Application.Review;

public sealed class ReviewPromptBuilder : IReviewPromptBuilder
{
    public LlmReviewPrompt Build(
        GitDiffPreviewResponse diff,
        IReadOnlyList<ReviewFinding> findings,
        GpuSearchReviewContext? gpuSearchContext = null)
    {
        return new LlmReviewPrompt(diff.RepoPath, findings, Truncate(diff.Diff, 24_000), gpuSearchContext);
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : string.Concat(value.AsSpan(0, maxLength), "\n\n[diff truncated]");
    }
}
