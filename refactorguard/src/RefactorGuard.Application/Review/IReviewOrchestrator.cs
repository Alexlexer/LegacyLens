namespace RefactorGuard.Application.Review;

public interface IReviewOrchestrator
{
    Task<DiffReviewReport> ReviewDiffAsync(
        DiffReviewRequest request,
        CancellationToken cancellationToken);
}
