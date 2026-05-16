using LegacyLens.Application.Search;
using LegacyLens.Domain.Search;

namespace LegacyLens.Application.Tests;

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
            => Task.FromResult(new GpuSearchHealth("ok"));

        public Task<GpuSearchStats> GetStatsAsync(CancellationToken cancellationToken)
            => Task.FromResult(new GpuSearchStats("ok", "cuda", "RTX 4060", 100));

        public Task<IReadOnlyList<GpuSearchResult>> SearchCodeAsync(CodeSearchRequest request, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<GpuSearchResult>>([]);

        public Task<IReadOnlyList<GpuSearchResult>> SearchSemanticAsync(CodeSearchRequest request, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<GpuSearchResult>>([]);

        public Task<IReadOnlyList<GpuSearchResult>> SearchHybridAsync(SearchHybridRequest request, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<GpuSearchResult>>([]);

        public Task<ReadBlockResponse> ReadBlockAsync(ReadBlockRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new ReadBlockResponse("ok", request.Path, null, null, null, null, null));

        public Task<ReadSkeletonResponse> ReadSkeletonAsync(ReadSkeletonRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new ReadSkeletonResponse("ok", request.Path, null, null, null, null));

        public Task<DependencyImpactResponse> GetDependencyImpactAsync(DependencyImpactRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new DependencyImpactResponse("ok", request.Path, null, []));

        public Task<SignalScanResponse> ScanSignalsAsync(SignalScanRequest request, CancellationToken cancellationToken)
            => throw new HttpRequestException("Not found", null, System.Net.HttpStatusCode.NotFound);
    }

    private sealed class FailingGpuSearchClient : IGpuSearchClient
    {
        public Task<GpuSearchHealth> GetHealthAsync(CancellationToken cancellationToken)
            => throw new HttpRequestException("unavailable");

        public Task<GpuSearchStats> GetStatsAsync(CancellationToken cancellationToken)
            => throw new HttpRequestException("unavailable");

        public Task<IReadOnlyList<GpuSearchResult>> SearchCodeAsync(CodeSearchRequest request, CancellationToken cancellationToken)
            => throw new HttpRequestException("unavailable");

        public Task<IReadOnlyList<GpuSearchResult>> SearchSemanticAsync(CodeSearchRequest request, CancellationToken cancellationToken)
            => throw new HttpRequestException("unavailable");

        public Task<IReadOnlyList<GpuSearchResult>> SearchHybridAsync(SearchHybridRequest request, CancellationToken cancellationToken)
            => throw new HttpRequestException("unavailable");

        public Task<ReadBlockResponse> ReadBlockAsync(ReadBlockRequest request, CancellationToken cancellationToken)
            => throw new HttpRequestException("unavailable");

        public Task<ReadSkeletonResponse> ReadSkeletonAsync(ReadSkeletonRequest request, CancellationToken cancellationToken)
            => throw new HttpRequestException("unavailable");

        public Task<DependencyImpactResponse> GetDependencyImpactAsync(DependencyImpactRequest request, CancellationToken cancellationToken)
            => throw new HttpRequestException("unavailable");

        public Task<SignalScanResponse> ScanSignalsAsync(SignalScanRequest request, CancellationToken cancellationToken)
            => throw new HttpRequestException("unavailable");
    }
}
