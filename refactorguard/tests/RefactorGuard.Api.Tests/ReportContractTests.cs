using RefactorGuard.Application.Reports;

namespace RefactorGuard.Api.Tests;

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
}
