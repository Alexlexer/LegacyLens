namespace LegacyLens.Application.DotNetAnalysis;

public interface IDotNetWorkspaceDiscovery
{
    Task<DotNetWorkspaceDiscoveryResult> DiscoverAsync(
        string repoRoot,
        CancellationToken cancellationToken);
}
