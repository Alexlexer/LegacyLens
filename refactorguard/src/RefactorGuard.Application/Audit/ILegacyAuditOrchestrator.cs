namespace RefactorGuard.Application.Audit;

public interface ILegacyAuditOrchestrator
{
    Task<LegacyAuditReport> AuditAsync(
        LegacyAuditRequest request,
        CancellationToken cancellationToken);
}
