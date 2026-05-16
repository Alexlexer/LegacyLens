namespace LegacyLens.Application.Audit;

public interface IAuditProvider
{
    string Name { get; }

    Task<AuditProviderResult> AnalyzeAsync(
        AuditContext context,
        CancellationToken cancellationToken);
}
