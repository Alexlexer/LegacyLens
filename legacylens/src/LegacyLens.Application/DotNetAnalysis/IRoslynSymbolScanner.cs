namespace LegacyLens.Application.DotNetAnalysis;

public interface IRoslynSymbolScanner
{
    Task<DotNetWorkspaceScanResponse> ScanAsync(
        string repoRoot,
        CancellationToken cancellationToken);
}
