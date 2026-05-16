using LegacyLens.Application.Audit;
using LegacyLens.Application.Review;

namespace LegacyLens.Application.Reports;

public interface IReportRepository
{
    Task SaveAsync(DiffReviewReport report, CancellationToken cancellationToken);

    Task<DiffReviewReport?> GetByIdAsync(string reportId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReportSummary>> ListAsync(CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string reportId, CancellationToken cancellationToken);

    Task SaveAuditAsync(LegacyAuditReport report, CancellationToken cancellationToken);

    Task<LegacyAuditReport?> GetAuditByIdAsync(string reportId, CancellationToken cancellationToken);
}
