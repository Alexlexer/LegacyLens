using LegacyLens.Application.Audit;
using LegacyLens.Application.DotNetAnalysis;
using LegacyLens.Application.Search;
using LegacyLens.Domain.Search;

namespace LegacyLens.Application.Tests.Audit;

public sealed class GpuSearchSignalAuditProviderTests
{
    [Fact]
    public async Task AnalyzeAsync_IndexesSelectedRepoBeforeSignalScan_WhenPatternNotReady()
    {
        var repoPath = Path.Combine(Path.GetTempPath(), "LegacyLensGpuSearchRepo");
        var client = new IndexingGpuSearchClient(repoPath, initiallyReady: false);
        var provider = new GpuSearchSignalAuditProvider(client, new GpuSearchAuditOptions());

        var result = await provider.AnalyzeAsync(CreateContext(repoPath), CancellationToken.None);

        Assert.True(client.IndexRootCalled);
        Assert.True(client.ScanSignalsCalled);
        Assert.NotNull(result.GpuSearchSummary);
        Assert.Equal("indexed selected repository", result.GpuSearchSummary!.IndexStatus);
        Assert.Equal(repoPath, result.GpuSearchSummary.IndexedRoot);
    }

    [Fact]
    public async Task AnalyzeAsync_SkipsSignalScan_WhenIndexingFails()
    {
        var repoPath = Path.Combine(Path.GetTempPath(), "LegacyLensGpuSearchRepo");
        var client = new IndexingGpuSearchClient(repoPath, initiallyReady: false) { FailIndexing = true };
        var provider = new GpuSearchSignalAuditProvider(client, new GpuSearchAuditOptions());

        var result = await provider.AnalyzeAsync(CreateContext(repoPath), CancellationToken.None);

        Assert.True(client.IndexRootCalled);
        Assert.False(client.ScanSignalsCalled);
        Assert.Contains(result.RiskFindings!, f => f.Code == "gpu-search-indexing-failed");
        Assert.Equal("indexing failed", result.GpuSearchSummary!.IndexStatus);
    }

    [Fact]
    public async Task AnalyzeAsync_DoesNotIndex_WhenParentRootAlreadyIndexedAndPatternReady()
    {
        var parent = Path.Combine(Path.GetTempPath(), "LegacyLensParentRoot");
        var repoPath = Path.Combine(parent, "Repo");
        var client = new IndexingGpuSearchClient(parent, initiallyReady: true);
        var provider = new GpuSearchSignalAuditProvider(client, new GpuSearchAuditOptions());

        var result = await provider.AnalyzeAsync(CreateContext(repoPath), CancellationToken.None);

        Assert.False(client.IndexRootCalled);
        Assert.True(client.ScanSignalsCalled);
        Assert.Equal(parent, result.GpuSearchSummary!.IndexedRoot);
    }

    private static AuditContext CreateContext(string repoPath)
    {
        return new AuditContext(
            repoPath,
            new LegacyAuditRequest(repoPath, IncludeRoslyn: false, IncludeGpuSearch: true, IncludeDependencyInjection: false),
            new DotNetWorkspaceDiscoveryResult([], null, []),
            new AuditWorkspaceSummary(null, null, 0, 0, 0, 0, []),
            [],
            [],
            [],
            null,
            null,
            null);
    }

    private sealed class IndexingGpuSearchClient(string indexedRoot, bool initiallyReady) : IGpuSearchClient
    {
        private bool _ready = initiallyReady;

        public bool IndexRootCalled { get; private set; }
        public bool ScanSignalsCalled { get; private set; }
        public bool FailIndexing { get; init; }

        public Task<GpuSearchHealth> GetHealthAsync(CancellationToken cancellationToken) => Task.FromResult(new GpuSearchHealth("ok"));
        public Task<GpuSearchStats> GetStatsAsync(CancellationToken cancellationToken) => Task.FromResult(new GpuSearchStats("ok", null, null, null));
        public Task<IReadOnlyList<GpuSearchResult>> SearchCodeAsync(CodeSearchRequest request, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<GpuSearchResult>>([]);
        public Task<IReadOnlyList<GpuSearchResult>> SearchSemanticAsync(CodeSearchRequest request, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<GpuSearchResult>>([]);
        public Task<IReadOnlyList<GpuSearchResult>> SearchHybridAsync(SearchHybridRequest request, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<GpuSearchResult>>([]);
        public Task<ReadBlockResponse> ReadBlockAsync(ReadBlockRequest request, CancellationToken cancellationToken) => Task.FromResult(new ReadBlockResponse("ok", string.Empty, null, null, null, null, null));
        public Task<ReadSkeletonResponse> ReadSkeletonAsync(ReadSkeletonRequest request, CancellationToken cancellationToken) => Task.FromResult(new ReadSkeletonResponse("ok", string.Empty, null, null, null, null));
        public Task<DependencyImpactResponse> GetDependencyImpactAsync(DependencyImpactRequest request, CancellationToken cancellationToken) => Task.FromResult(new DependencyImpactResponse("ok", string.Empty, null, []));

        public Task<GpuSearchIndexStatusResponse> GetIndexStatusAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new GpuSearchIndexStatusResponse
            {
                IndexedRoots = [System.Text.Json.JsonDocument.Parse($"\"{indexedRoot.Replace("\\", "\\\\")}\"").RootElement.Clone()],
                Pattern = new GpuSearchIndexComponentStatus { Ready = _ready, Files = _ready ? 10 : 0 },
                Dependency = new GpuSearchIndexComponentStatus { Ready = _ready },
                Semantic = new GpuSearchIndexComponentStatus { Ready = false },
                Status = _ready ? "ok" : "not_ready"
            });
        }

        public Task<GpuSearchIndexRootResponse> IndexRootAsync(GpuSearchIndexRootRequest request, CancellationToken cancellationToken)
        {
            IndexRootCalled = true;
            if (FailIndexing)
                throw new HttpRequestException("index failed");

            _ready = true;
            return Task.FromResult(new GpuSearchIndexRootResponse
            {
                Ok = true,
                Directory = request.Directory,
                NormalizedDirectory = request.Directory,
                Started = true,
                Completed = true,
                Pattern = new GpuSearchIndexComponentStatus { Ready = true, Files = 10 },
                Dependency = new GpuSearchIndexComponentStatus { Ready = true },
                Semantic = new GpuSearchIndexComponentStatus { Requested = request.IncludeSemantic, Ready = false },
                Message = "indexed"
            });
        }

        public Task<SignalScanResponse> ScanSignalsAsync(SignalScanRequest request, CancellationToken cancellationToken)
        {
            ScanSignalsCalled = true;
            return Task.FromResult(new SignalScanResponse(
                "ok",
                [],
                new SignalScanSummary(0, 0, new Dictionary<string, int>()),
                [],
                null,
                null));
        }
    }
}
