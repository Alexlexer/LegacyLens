using LegacyLens.Domain.Git;

namespace LegacyLens.Application.Git;

public interface IGitDiffService
{
    Task<GitDiffPreviewResponse> GetCurrentDiffAsync(
        GitDiffPreviewRequest request,
        CancellationToken cancellationToken);
}
