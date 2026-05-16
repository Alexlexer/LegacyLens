using LegacyLens.Application.DotNetAnalysis;

namespace LegacyLens.Infrastructure.DotNetAnalysis;

public interface IRoslynWorkspaceCache
{
    Task<RoslynWorkspaceCacheLease> GetOrLoadAsync(
        string repoPath,
        CancellationToken cancellationToken);

    RoslynWorkspaceCacheStatus GetStatus(string repoPath);

    void Invalidate(string repoPath);

    void Clear();
}

public sealed record RoslynWorkspaceCacheStatus(
    string RepoPath,
    bool IsCached,
    string? WorkspacePath,
    DotNetWorkspaceKind? WorkspaceKind,
    DateTimeOffset? LoadedAtUtc,
    DateTimeOffset? LastAccessedUtc,
    int ProjectCount,
    int DocumentCount,
    IReadOnlyList<string> Diagnostics,
    IReadOnlyList<string> Warnings,
    string? SourceFingerprint,
    long HitCount);

public sealed record RoslynWorkspaceCacheLease(
    DotNetWorkspaceDiscoveryResult DiscoveryResult,
    RoslynWorkspaceLoader.LoadedRoslynWorkspace Workspace,
    bool IsFromCache,
    string SourceFingerprint,
    bool DisposeOnRelease = false) : IDisposable
{
    public void Dispose()
    {
        if (DisposeOnRelease)
            Workspace.Dispose();
    }
}
