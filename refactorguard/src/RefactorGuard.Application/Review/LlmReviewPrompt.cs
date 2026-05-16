namespace RefactorGuard.Application.Review;

public sealed record LlmReviewPrompt(
    string RepoPath,
    IReadOnlyList<ReviewFinding> Findings,
    string Diff,
    GpuSearchReviewContext? GpuSearchContext = null,
    RoslynReviewContext? RoslynContext = null);
