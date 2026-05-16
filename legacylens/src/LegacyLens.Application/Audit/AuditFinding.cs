namespace LegacyLens.Application.Audit;

public sealed record AuditFinding(
    string Severity,
    string Code,
    string Title,
    string Message,
    string? FilePath = null,
    int? Line = null,
    string? Evidence = null);
