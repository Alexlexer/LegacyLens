using RefactorGuard.Application.Review;

namespace RefactorGuard.Api.Tests;

public sealed class DiffReviewContractTests
{
    [Fact]
    public void DiffReviewReport_ExposesStableContract()
    {
        var report = new DiffReviewReport(
            "id",
            "repo",
            DateTimeOffset.UnixEpoch,
            0,
            [],
            [new ReviewFinding("empty-diff", "Info", null, "No diff", "No changes")],
            "# Report",
            "LLM summary",
            "LmStudio");

        Assert.Equal("id", report.ReportId);
        Assert.Equal("repo", report.RepoPath);
        Assert.Single(report.Findings);
        Assert.Equal("# Report", report.Markdown);
        Assert.Equal("LLM summary", report.LlmSummary);
        Assert.Equal("LmStudio", report.LlmProvider);
    }
}
