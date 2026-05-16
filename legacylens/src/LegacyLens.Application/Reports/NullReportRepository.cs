using LegacyLens.Application.Audit;
using LegacyLens.Application.Review;

namespace LegacyLens.Application.Reports;

public sealed class NullReportRepository : IReportRepository
{
    public Task SaveAsync(DiffReviewReport report, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task<DiffReviewReport?> GetByIdAsync(string reportId, CancellationToken cancellationToken)
        => Task.FromResult<DiffReviewReport?>(null);

    public Task<IReadOnlyList<ReportSummary>> ListAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<ReportSummary>>([]);

    public Task<bool> DeleteAsync(string reportId, CancellationToken cancellationToken)
        => Task.FromResult(false);

    public Task SaveAuditAsync(LegacyAuditReport report, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task<LegacyAuditReport?> GetAuditByIdAsync(string reportId, CancellationToken cancellationToken)
        => Task.FromResult<LegacyAuditReport?>(null);
}
