using LegacyLens.Domain.Search;

namespace LegacyLens.Application.Search;

public interface IGpuSearchClient
{
    Task<GpuSearchHealth> GetHealthAsync(CancellationToken cancellationToken);

    Task<GpuSearchStats> GetStatsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<GpuSearchResult>> SearchCodeAsync(
        CodeSearchRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<GpuSearchResult>> SearchSemanticAsync(
        CodeSearchRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<GpuSearchResult>> SearchHybridAsync(
        SearchHybridRequest request,
        CancellationToken cancellationToken);

    Task<ReadBlockResponse> ReadBlockAsync(
        ReadBlockRequest request,
        CancellationToken cancellationToken);

    Task<ReadSkeletonResponse> ReadSkeletonAsync(
        ReadSkeletonRequest request,
        CancellationToken cancellationToken);

    Task<DependencyImpactResponse> GetDependencyImpactAsync(
        DependencyImpactRequest request,
        CancellationToken cancellationToken);

    Task<GpuSearchIndexRootResponse> IndexRootAsync(
        GpuSearchIndexRootRequest request,
        CancellationToken cancellationToken)
        => throw new HttpRequestException("gpu-search index root is not implemented by this client.", null, System.Net.HttpStatusCode.NotFound);

    Task<GpuSearchIndexStatusResponse> GetIndexStatusAsync(
        CancellationToken cancellationToken)
        => throw new HttpRequestException("gpu-search index status is not implemented by this client.", null, System.Net.HttpStatusCode.NotFound);

    Task<SignalScanResponse> ScanSignalsAsync(
        SignalScanRequest request,
        CancellationToken cancellationToken);
}

