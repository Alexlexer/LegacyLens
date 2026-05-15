using RefactorGuard.Application.Git;
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
            new StubReviewLlmProvider("LLM summary"));

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
            new StubReviewLlmProvider("LLM summary"));

        var report = await orchestrator.ReviewDiffAsync(new DiffReviewRequest("repo"), CancellationToken.None);

        Assert.Contains(report.Findings, finding => finding.RuleId == "empty-diff");
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
}
