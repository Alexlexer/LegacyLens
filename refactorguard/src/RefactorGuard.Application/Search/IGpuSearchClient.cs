using RefactorGuard.Domain.Search;

namespace RefactorGuard.Application.Search;

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
}
