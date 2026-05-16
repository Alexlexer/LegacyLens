using LegacyLens.Domain.Git;

namespace LegacyLens.Api.Tests;

public sealed class DiffPreviewContractTests
{
    [Fact]
    public void GitDiffPreviewResponse_ExposesStableContract()
    {
        var response = new GitDiffPreviewResponse(
            "repo",
            1,
            [new GitDiffFile("sample.txt", "M", 2, 1)],
            "diff");

        Assert.Equal("repo", response.RepoPath);
        Assert.Equal(1, response.ChangedFileCount);
        Assert.Equal("sample.txt", response.Files[0].Path);
        Assert.Equal("diff", response.Diff);
    }
}
