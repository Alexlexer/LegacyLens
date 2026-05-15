using RefactorGuard.Domain.Git;

namespace RefactorGuard.Application.Git;

public interface IGitDiffService
{
    Task<GitDiffPreviewResponse> GetCurrentDiffAsync(
        GitDiffPreviewRequest request,
        CancellationToken cancellationToken);
}
