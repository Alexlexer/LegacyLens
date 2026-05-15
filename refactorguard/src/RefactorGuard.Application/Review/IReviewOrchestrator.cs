namespace RefactorGuard.Application.Review;

public interface IReviewOrchestrator
{
    Task<string> ReviewDiffAsync(CancellationToken cancellationToken);
}
