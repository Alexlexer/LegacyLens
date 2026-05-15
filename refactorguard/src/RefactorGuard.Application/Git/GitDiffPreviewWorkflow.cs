using RefactorGuard.Domain.Git;

namespace RefactorGuard.Application.Git;

public sealed class GitDiffPreviewWorkflow(IGitDiffService gitDiffService)
{
    public Task<GitDiffPreviewResponse> PreviewAsync(
        GitDiffPreviewRequest request,
        CancellationToken cancellationToken)
    {
        return gitDiffService.GetCurrentDiffAsync(request, cancellationToken);
    }
}
