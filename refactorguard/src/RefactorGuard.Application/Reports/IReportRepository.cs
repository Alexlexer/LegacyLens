using RefactorGuard.Application.Review;

namespace RefactorGuard.Application.Reports;

public interface IReportRepository
{
    Task SaveAsync(DiffReviewReport report, CancellationToken cancellationToken);

    Task<DiffReviewReport?> GetByIdAsync(string reportId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReportSummary>> ListAsync(CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string reportId, CancellationToken cancellationToken);
}
