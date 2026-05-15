using RefactorGuard.Application.Review;

namespace RefactorGuard.Application.Reports;

public sealed class NullReportRepository : IReportRepository
{
    public Task SaveAsync(DiffReviewReport report, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<DiffReviewReport?> GetByIdAsync(string reportId, CancellationToken cancellationToken)
    {
        return Task.FromResult<DiffReviewReport?>(null);
    }

    public Task<IReadOnlyList<ReportSummary>> ListAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<ReportSummary>>([]);
    }

    public Task<bool> DeleteAsync(string reportId, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }
}
