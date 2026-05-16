namespace LegacyLens.Application.Audit;

public interface ILegacyAuditMarkdownFormatter
{
    string Format(LegacyAuditReport report);
}
