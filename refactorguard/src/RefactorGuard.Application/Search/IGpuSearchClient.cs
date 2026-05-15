using RefactorGuard.Domain.Search;

namespace RefactorGuard.Application.Search;

public interface IGpuSearchClient
{
    Task<GpuSearchHealth> GetHealthAsync(CancellationToken cancellationToken);

    Task<GpuSearchStats> GetStatsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<SearchResult>> SearchHybridAsync(
        SearchHybridRequest request,
        CancellationToken cancellationToken);
}
