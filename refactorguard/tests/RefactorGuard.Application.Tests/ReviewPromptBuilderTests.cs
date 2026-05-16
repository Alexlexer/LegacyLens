using RefactorGuard.Application.DotNetAnalysis;
using RefactorGuard.Application.Review;
using RefactorGuard.Domain.Git;

namespace RefactorGuard.Application.Tests;

public sealed class ReviewPromptBuilderTests
{
    [Fact]
    public void Build_IncludesRepoPathFindingsAndDiff()
    {
        var diff = new GitDiffPreviewResponse(
            "repo",
            1,
            [new GitDiffFile("src/App.cs", "M", 1, 0)],
            "+change");
        var findings = new[]
        {
            new ReviewFinding("rule", "Info", "src/App.cs", "Title", "Description")
        };

        var prompt = new ReviewPromptBuilder().Build(diff, findings);

        Assert.Equal("repo", prompt.RepoPath);
        Assert.Equal(findings, prompt.Findings);
        Assert.Equal("+change", prompt.Diff);
    }

    [Fact]
    public void Build_TruncatesVeryLargeDiff()
    {
        var diffText = new string('a', 25_000);
        var diff = new GitDiffPreviewResponse("repo", 1, [], diffText);

        var prompt = new ReviewPromptBuilder().Build(diff, []);

        Assert.True(prompt.Diff.Length < diffText.Length);
        Assert.Contains("[diff truncated]", prompt.Diff);
    }

    [Fact]
    public void Build_IncludesGpuSearchContext_WhenProvided()
    {
        var diff = new GitDiffPreviewResponse("repo", 1, [], "+change");
        var context = new GpuSearchReviewContext(
            true,
            [new ChangedFileContext("src/App.cs", null, null, [])]);

        var prompt = new ReviewPromptBuilder().Build(diff, [], context);

        Assert.NotNull(prompt.GpuSearchContext);
        Assert.True(prompt.GpuSearchContext!.WasAvailable);
        Assert.Single(prompt.GpuSearchContext.Files);
    }

    [Fact]
    public void Build_LeavesGpuSearchContextNull_WhenNotProvided()
    {
        var diff = new GitDiffPreviewResponse("repo", 1, [], "+change");

        var prompt = new ReviewPromptBuilder().Build(diff, []);

        Assert.Null(prompt.GpuSearchContext);
    }

    [Fact]
    public void Build_IncludesRoslynContext_WhenProvided()
    {
        var diff = new GitDiffPreviewResponse("repo", 1, [], "+change");
        var roslynContext = new RoslynReviewContext(
            true,
            "/workspace/App.sln",
            DotNetWorkspaceKind.Sln,
            [new ChangedSymbolSummary("UserService", "SampleApp.UserService", "class", "src/UserService.cs", 5, 1, "SampleApp")],
            [],
            [],
            null);

        var prompt = new ReviewPromptBuilder().Build(diff, [], roslynContext: roslynContext);

        Assert.NotNull(prompt.RoslynContext);
        Assert.True(prompt.RoslynContext!.Success);
        Assert.Single(prompt.RoslynContext.ChangedSymbols);
    }

    [Fact]
    public void Build_LeavesRoslynContextNull_WhenNotProvided()
    {
        var diff = new GitDiffPreviewResponse("repo", 1, [], "+change");

        var prompt = new ReviewPromptBuilder().Build(diff, []);

        Assert.Null(prompt.RoslynContext);
    }
}
