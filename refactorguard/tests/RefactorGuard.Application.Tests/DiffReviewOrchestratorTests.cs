using RefactorGuard.Application.Git;
using RefactorGuard.Application.Reports;
using RefactorGuard.Application.Review;
using RefactorGuard.Domain.Git;

namespace RefactorGuard.Application.Tests;

public sealed class DiffReviewOrchestratorTests
{
    [Fact]
    public async Task ReviewDiffAsync_ReturnsMarkdownReportWithFindings()
    {
        var diff = new GitDiffPreviewResponse(
            "repo",
            2,
            [
                new GitDiffFile("src/App.cs", "M", 5, 1),
                new GitDiffFile("tests/AppTests.cs", "M", 10, 0)
            ],
            "diff");
        var orchestrator = new DiffReviewOrchestrator(
            new StubGitDiffService(diff),
            new MarkdownReviewReportFormatter(),
            new ReviewPromptBuilder(),
            new StubReviewLlmProvider("LLM summary"),
            new StubReportRepository());

        var report = await orchestrator.ReviewDiffAsync(new DiffReviewRequest("repo"), CancellationToken.None);

        Assert.Equal("repo", report.RepoPath);
        Assert.Equal(2, report.ChangedFileCount);
        Assert.Contains(report.Findings, finding => finding.RuleId == "test-change");
        Assert.Contains("# RefactorGuard Diff Review", report.Markdown);
    }

    [Fact]
    public async Task ReviewDiffAsync_AddsEmptyDiffFinding_WhenNoFilesChanged()
    {
        var diff = new GitDiffPreviewResponse("repo", 0, [], string.Empty);
        var orchestrator = new DiffReviewOrchestrator(
            new StubGitDiffService(diff),
            new MarkdownReviewReportFormatter(),
            new ReviewPromptBuilder(),
            new StubReviewLlmProvider("LLM summary"),
            new StubReportRepository());

        var report = await orchestrator.ReviewDiffAsync(new DiffReviewRequest("repo"), CancellationToken.None);

        Assert.Contains(report.Findings, finding => finding.RuleId == "empty-diff");
    }

    [Fact]
    public async Task ReviewDiffAsync_SavesReport()
    {
        var repository = new StubReportRepository();
        var diff = new GitDiffPreviewResponse("repo", 0, [], string.Empty);
        var orchestrator = new DiffReviewOrchestrator(
            new StubGitDiffService(diff),
            new MarkdownReviewReportFormatter(),
            new ReviewPromptBuilder(),
            new StubReviewLlmProvider("LLM summary"),
            repository);

        var report = await orchestrator.ReviewDiffAsync(new DiffReviewRequest("repo"), CancellationToken.None);

        Assert.Same(report, repository.SavedReport);
    }

    private sealed class StubGitDiffService(GitDiffPreviewResponse response) : IGitDiffService
    {
        public Task<GitDiffPreviewResponse> GetCurrentDiffAsync(
            GitDiffPreviewRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(response);
        }
    }

    private sealed class StubReviewLlmProvider(string summary) : IReviewLlmProvider
    {
        public string Name => "Stub";

        public Task<string?> GenerateReviewAsync(
            LlmReviewPrompt prompt,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<string?>(summary);
        }
    }

    private sealed class StubReportRepository : IReportRepository
    {
        public DiffReviewReport? SavedReport { get; private set; }

        public Task SaveAsync(DiffReviewReport report, CancellationToken cancellationToken)
        {
            SavedReport = report;
            return Task.CompletedTask;
        }

        public Task<DiffReviewReport?> GetByIdAsync(string reportId, CancellationToken cancellationToken)
        {
            return Task.FromResult<DiffReviewReport?>(SavedReport?.ReportId == reportId ? SavedReport : null);
        }

        public Task<IReadOnlyList<ReportSummary>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ReportSummary>>([]);
        }

        public Task<bool> DeleteAsync(string reportId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }
    }
}
