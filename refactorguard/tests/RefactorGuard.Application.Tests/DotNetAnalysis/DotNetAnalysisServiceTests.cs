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
        {
            return Task.FromResult(new GpuSearchHealth("ok"));
        }

        public Task<GpuSearchStats> GetStatsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new GpuSearchStats("ok", null, null, null));
        }

        public Task<IReadOnlyList<SearchResult>> SearchHybridAsync(
            SearchHybridRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<SearchResult>>(
                [new SearchResult("src/App.cs", 12, "Task.Result", 0.9)]);
        }
    }
}
