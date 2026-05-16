using LegacyLens.Application.Reports;

namespace LegacyLens.Api.Tests;

public sealed class ReportContractTests
{
    [Fact]
    public void ReportSummary_ExposesStableContract()
    {
        var summary = new ReportSummary(
            "report-1",
            "repo",
            DateTimeOffset.UnixEpoch,
            2,
            "Deterministic");

        Assert.Equal("report-1", summary.ReportId);
        Assert.Equal("repo", summary.RepoPath);
        Assert.Equal(2, summary.ChangedFileCount);
        Assert.Equal("Deterministic", summary.LlmProvider);
    }

    [Fact]
    public void ReportSummary_DefaultsReportTypeToDiffReview()
    {
        var summary = new ReportSummary("r", "repo", DateTimeOffset.UnixEpoch, 0, "Deterministic");

        Assert.Equal(ReportType.DiffReview, summary.ReportType);
        Assert.Null(summary.Title);
    }

    [Fact]
    public void ReportSummary_AcceptsLegacyAuditType()
    {
        var summary = new ReportSummary(
            "audit-1",
            @"C:\projects\MyApp",
            DateTimeOffset.UnixEpoch,
            0,
            "LLM",
            ReportType.LegacyAudit,
            "MyApp");

        Assert.Equal(ReportType.LegacyAudit, summary.ReportType);
        Assert.Equal("MyApp", summary.Title);
    }

    [Fact]
    public void ReportType_Constants_HaveExpectedValues()
    {
        Assert.Equal("DiffReview", ReportType.DiffReview);
        Assert.Equal("LegacyAudit", ReportType.LegacyAudit);
    }
}
