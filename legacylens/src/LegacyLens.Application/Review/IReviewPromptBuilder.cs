using LegacyLens.Domain.Git;

namespace LegacyLens.Application.Review;

public interface IReviewPromptBuilder
{
    LlmReviewPrompt Build(
        GitDiffPreviewResponse diff,
        IReadOnlyList<ReviewFinding> findings,
        GpuSearchReviewContext? gpuSearchContext = null,
        RoslynReviewContext? roslynContext = null);
}
