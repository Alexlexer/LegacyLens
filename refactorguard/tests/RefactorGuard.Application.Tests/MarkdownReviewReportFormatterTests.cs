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

    [Fact]
    public void Format_IncludesGpuSearchContext_WhenAvailable()
    {
        var context = new GpuSearchReviewContext(
            true,
            [
                new ChangedFileContext(
                    "src/UserService.cs",
                    new DependencyImpactSummary(3, ["src/AuthController.cs", "src/AdminController.cs"]),
                    new SkeletonSummary("public class UserService { }", "csharp"),
                    [new RelatedCodeResult("src/IUserService.cs", 10, 10, "interface snippet", "exact", 1.0)])
            ]);
        var report = new DiffReviewReport(
            "report-2",
            "repo",
            DateTimeOffset.UnixEpoch,
            1,
            [new GitDiffFile("src/UserService.cs", "M", 5, 1)],
            [],
            string.Empty,
            GpuSearchContext: context);

        var markdown = new MarkdownReviewReportFormatter().Format(report);

        Assert.Contains("gpu-search Context", markdown);
        Assert.Contains("src/UserService.cs", markdown);
        Assert.Contains("Impacted files: 3", markdown);
        Assert.Contains("src/AuthController.cs", markdown);
        Assert.Contains("src/IUserService.cs", markdown);
        Assert.Contains("UserService", markdown);
    }

    [Fact]
    public void Format_IncludesUnavailableWarning_WhenGpuSearchUnavailable()
    {
        var context = new GpuSearchReviewContext(false, [], "Connection refused");
        var report = new DiffReviewReport(
            "report-3",
            "repo",
            DateTimeOffset.UnixEpoch,
            0,
            [],
            [],
            string.Empty,
            GpuSearchContext: context);

        var markdown = new MarkdownReviewReportFormatter().Format(report);

        Assert.Contains("gpu-search Context", markdown);
        Assert.Contains("unavailable or returned errors", markdown);
        Assert.Contains("Connection refused", markdown);
    }

    [Fact]
    public void Format_OmitsGpuSearchSection_WhenContextIsNull()
    {
        var report = new DiffReviewReport(
            "report-4",
            "repo",
            DateTimeOffset.UnixEpoch,
            0,
            [],
            [],
            string.Empty);

        var markdown = new MarkdownReviewReportFormatter().Format(report);

        Assert.DoesNotContain("gpu-search Context", markdown);
    }

    [Fact]
    public void Format_ShowsConfidenceAndAnalysisMode_WhenPresent()
    {
        var context = new GpuSearchReviewContext(
            true,
            [
                new ChangedFileContext(
                    "src/UserService.cs",
                    new DependencyImpactSummary(
                        2,
                        ["src/AuthController.cs"],
                        Confidence: "medium",
                        AnalysisMode: "heuristic"),
                    null,
                    [])
            ]);
        var report = new DiffReviewReport(
            "report-5",
            "repo",
            DateTimeOffset.UnixEpoch,
            1,
            [new GitDiffFile("src/UserService.cs", "M", 5, 1)],
            [],
            string.Empty,
            GpuSearchContext: context);

        var markdown = new MarkdownReviewReportFormatter().Format(report);

        Assert.Contains("Confidence: medium", markdown);
        Assert.Contains("Analysis mode: heuristic", markdown);
    }

    [Fact]
    public void Format_ShowsWarningsAndLimitations_WhenPresent()
    {
        var context = new GpuSearchReviewContext(
            true,
            [
                new ChangedFileContext(
                    "src/UserService.cs",
                    new DependencyImpactSummary(
                        0,
                        [],
                        Confidence: "low",
                        AnalysisMode: "heuristic",
                        Limitations: ["C# analysis does not use Roslyn.", "Dynamic dispatch not tracked."],
                        Warnings: ["file not present in graph"]),
                    null,
                    [])
            ]);
        var report = new DiffReviewReport(
            "report-6",
            "repo",
            DateTimeOffset.UnixEpoch,
            1,
            [new GitDiffFile("src/UserService.cs", "M", 2, 0)],
            [],
            string.Empty,
            GpuSearchContext: context);

        var markdown = new MarkdownReviewReportFormatter().Format(report);

        Assert.Contains("Warning: file not present in graph", markdown);
        Assert.Contains("C# analysis does not use Roslyn.", markdown);
        Assert.Contains("Dynamic dispatch not tracked.", markdown);
        Assert.Contains("advisory only", markdown);
    }

    [Fact]
    public void Format_ShowsDependencyImpactReasons_WhenPresent()
    {
        var context = new GpuSearchReviewContext(
            true,
            [
                new ChangedFileContext(
                    "src/UserService.cs",
                    new DependencyImpactSummary(
                        1,
                        ["src/UserController.cs"],
                        ImpactedFiles:
                        [
                            new DependencyImpactedFile(
                                "src/UserController.cs",
                                1,
                                "references type UserService")
                        ]),
                    null,
                    [])
            ]);
        var report = new DiffReviewReport(
            "report-8",
            "repo",
            DateTimeOffset.UnixEpoch,
            1,
            [new GitDiffFile("src/UserService.cs", "M", 2, 0)],
            [],
            string.Empty,
            GpuSearchContext: context);

        var markdown = new MarkdownReviewReportFormatter().Format(report);

        Assert.Contains("src/UserController.cs", markdown);
        Assert.Contains("references type UserService", markdown);
    }

    [Fact]
    public void Format_OmitsLimitationsBlock_WhenLimitationsAbsent()
    {
        var context = new GpuSearchReviewContext(
            true,
            [
                new ChangedFileContext(
                    "src/UserService.cs",
                    new DependencyImpactSummary(1, ["src/Controller.cs"]),
                    null,
                    [])
            ]);
        var report = new DiffReviewReport(
            "report-7",
            "repo",
            DateTimeOffset.UnixEpoch,
            1,
            [new GitDiffFile("src/UserService.cs", "M", 2, 0)],
            [],
            string.Empty,
            GpuSearchContext: context);

        var markdown = new MarkdownReviewReportFormatter().Format(report);

        Assert.DoesNotContain("limitations", markdown, StringComparison.OrdinalIgnoreCase);
    }
}
