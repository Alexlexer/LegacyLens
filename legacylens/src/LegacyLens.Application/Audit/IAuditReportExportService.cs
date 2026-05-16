namespace LegacyLens.Application.Audit;

public interface IAuditReportExportService
{
    string ExportMarkdown(LegacyAuditReport report);

    string ExportHtml(LegacyAuditReport report);

    string BuildFileName(LegacyAuditReport report, string extension);
}
