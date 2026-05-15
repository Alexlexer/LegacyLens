using RefactorGuard.Domain.Git;

namespace RefactorGuard.Application.Review;

public interface IReviewPromptBuilder
{
    LlmReviewPrompt Build(
        GitDiffPreviewResponse diff,
        IReadOnlyList<ReviewFinding> findings,
        GpuSearchReviewContext? gpuSearchContext = null);
}
