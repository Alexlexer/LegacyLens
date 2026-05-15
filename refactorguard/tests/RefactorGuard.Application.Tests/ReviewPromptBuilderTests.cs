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
}
