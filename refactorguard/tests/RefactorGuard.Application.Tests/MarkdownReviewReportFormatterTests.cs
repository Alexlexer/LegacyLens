using RefactorGuard.Application.Review;
using RefactorGuard.Domain.Git;

namespace RefactorGuard.Application.Tests;

public sealed class MarkdownReviewReportFormatterTests
{
    [Fact]
    public void Format_IncludesFilesAndFindings()
    {
        var report = new DiffReviewReport(
            "report-1",
            "repo",
            DateTimeOffset.UnixEpoch,
            1,
            [new GitDiffFile("src/App.cs", "M", 2, 1)],
            [new ReviewFinding("rule", "Info", "src/App.cs", "Title", "Description")],
            string.Empty,
            "LLM summary",
            "LmStudio");

        var markdown = new MarkdownReviewReportFormatter().Format(report);

        Assert.Contains("report-1", markdown);
        Assert.Contains("src/App.cs", markdown);
        Assert.Contains("Title", markdown);
        Assert.Contains("LLM summary", markdown);
    }
}
