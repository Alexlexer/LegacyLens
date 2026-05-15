namespace RefactorGuard.Application.Reports;

public sealed record ReportSummary(
    string ReportId,
    string RepoPath,
    DateTimeOffset GeneratedAtUtc,
    int ChangedFileCount,
    string LlmProvider);
