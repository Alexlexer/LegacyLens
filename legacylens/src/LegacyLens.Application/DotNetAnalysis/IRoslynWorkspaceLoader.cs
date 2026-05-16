namespace LegacyLens.Application.DotNetAnalysis;

public interface IRoslynWorkspaceLoader
{
    Task<RoslynWorkspaceLoadResult> LoadAsync(
        DotNetWorkspaceDiscoveryResult discoveryResult,
        CancellationToken cancellationToken);
}
