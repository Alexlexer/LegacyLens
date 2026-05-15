using RefactorGuard.Application.Git;
using RefactorGuard.Domain.Git;

namespace RefactorGuard.Application.Tests;

public sealed class GitDiffPreviewWorkflowTests
{
    [Fact]
    public async Task PreviewAsync_DelegatesToGitDiffService()
    {
        var expected = new GitDiffPreviewResponse("repo", 0, [], string.Empty);
        var service = new StubGitDiffService(expected);
        var workflow = new GitDiffPreviewWorkflow(service);

        var result = await workflow.PreviewAsync(new GitDiffPreviewRequest("repo"), CancellationToken.None);

        Assert.Same(expected, result);
        Assert.True(service.WasCalled);
    }

    private sealed class StubGitDiffService(GitDiffPreviewResponse response) : IGitDiffService
    {
        public bool WasCalled { get; private set; }

        public Task<GitDiffPreviewResponse> GetCurrentDiffAsync(
            GitDiffPreviewRequest request,
            CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(response);
        }
    }
}
