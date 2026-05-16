namespace RefactorGuard.Application.Audit;

public interface ILegacyAuditMarkdownFormatter
{
    string Format(LegacyAuditReport report);
}
