namespace LegacyLens.Application.Reports;

public sealed record ReportSummary(
    string ReportId,
    string RepoPath,
    DateTimeOffset GeneratedAtUtc,
    int ChangedFileCount,
    string LlmProvider,
    string ReportType = Reports.ReportType.DiffReview,
    string? Title = null);
