using RefactorGuard.Application.Search;
using RefactorGuard.Domain.Search;

namespace RefactorGuard.Application.Tests;

public sealed class GpuSearchStatusWorkflowTests
{
    [Fact]
    public async Task GetStatusAsync_ReturnsAvailable_WhenClientSucceeds()
    {
        var workflow = new GpuSearchStatusWorkflow(new StubGpuSearchClient());

        var status = await workflow.GetStatusAsync(CancellationToken.None);

        Assert.True(status.IsAvailable);
        Assert.Equal("ok", status.Health?.Status);
        Assert.Equal("cuda", status.Stats?.Backend);
        Assert.Null(status.Error);
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsUnavailable_WhenClientFails()
    {
        var workflow = new GpuSearchStatusWorkflow(new FailingGpuSearchClient());

        var status = await workflow.GetStatusAsync(CancellationToken.None);

        Assert.False(status.IsAvailable);
        Assert.NotNull(status.Error);
    }

    private sealed class StubGpuSearchClient : IGpuSearchClient
    {
        public Task<GpuSearchHealth> GetHealthAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new GpuSearchHealth("ok"));
        }

        public Task<GpuSearchStats> GetStatsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new GpuSearchStats("ok", "cuda", "RTX 4060", 100));
        }

        public Task<IReadOnlyList<SearchResult>> SearchHybridAsync(
            SearchHybridRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<SearchResult>>([]);
        }
    }

    private sealed class FailingGpuSearchClient : IGpuSearchClient
    {
        public Task<GpuSearchHealth> GetHealthAsync(CancellationToken cancellationToken)
        {
            throw new HttpRequestException("unavailable");
        }

        public Task<GpuSearchStats> GetStatsAsync(CancellationToken cancellationToken)
        {
            throw new HttpRequestException("unavailable");
        }

        public Task<IReadOnlyList<SearchResult>> SearchHybridAsync(
            SearchHybridRequest request,
            CancellationToken cancellationToken)
        {
            throw new HttpRequestException("unavailable");
        }
    }
}
