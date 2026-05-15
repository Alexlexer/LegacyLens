using RefactorGuard.Application.DotNetAnalysis;
using RefactorGuard.Application.Search;
using RefactorGuard.Domain.Search;

namespace RefactorGuard.Application.Tests.DotNetAnalysis;

public sealed class DotNetAnalysisServiceTests
{
    [Fact]
    public async Task AnalyzeAsync_ReturnsFindingsFromGpuSearchResults()
    {
        var service = new DotNetAnalysisService(
            new DotNetAnalysisPresetCatalog(),
            new StubGpuSearchClient());

        var response = await service.AnalyzeAsync(
            new DotNetAnalysisRequest("repo", ["async-blocking"], 5),
            CancellationToken.None);

        Assert.Single(response.Presets);
        Assert.Single(response.Findings);
        Assert.Equal("async-blocking", response.Findings[0].PresetId);
        Assert.Equal("src/App.cs", response.Findings[0].FilePath);
    }

    private sealed class StubGpuSearchClient : IGpuSearchClient
    {
        public Task<GpuSearchHealth> GetHealthAsync(CancellationToken cancellationToken)
            => Task.FromResult(new GpuSearchHealth("ok"));

        public Task<GpuSearchStats> GetStatsAsync(CancellationToken cancellationToken)
            => Task.FromResult(new GpuSearchStats("ok", null, null, null));

        public Task<IReadOnlyList<GpuSearchResult>> SearchCodeAsync(CodeSearchRequest request, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<GpuSearchResult>>([]);

        public Task<IReadOnlyList<GpuSearchResult>> SearchSemanticAsync(CodeSearchRequest request, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<GpuSearchResult>>([]);

        public Task<IReadOnlyList<GpuSearchResult>> SearchHybridAsync(SearchHybridRequest request, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<GpuSearchResult>>(
                [new GpuSearchResult("src/App.cs", null, 12, null, 0.9, null, "Task.Result", "hybrid")]);

        public Task<ReadBlockResponse> ReadBlockAsync(ReadBlockRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new ReadBlockResponse("ok", request.Path, null, null, null, null, null));

        public Task<ReadSkeletonResponse> ReadSkeletonAsync(ReadSkeletonRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new ReadSkeletonResponse("ok", request.Path, null, null, null, null));

        public Task<DependencyImpactResponse> GetDependencyImpactAsync(DependencyImpactRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new DependencyImpactResponse("ok", request.Path, null, []));
    }
}
