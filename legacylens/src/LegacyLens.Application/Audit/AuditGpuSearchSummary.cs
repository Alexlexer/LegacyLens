namespace LegacyLens.Application.Audit;

public sealed record AuditGpuSearchResult(
    string Query,
    string? FilePath,
    int? Line,
    string? Snippet);

public sealed record AuditGpuSearchSummary(
    bool WasAvailable,
    int QueriesRun,
    int TotalResults,
    IReadOnlyList<AuditGpuSearchResult> Results,
    string? ErrorMessage,
    bool UsedSignalScan = false,
    IReadOnlyList<string>? SignalCategories = null,
    IReadOnlyList<string>? ScanLimitations = null,
    IReadOnlyList<string>? ScanWarnings = null);
