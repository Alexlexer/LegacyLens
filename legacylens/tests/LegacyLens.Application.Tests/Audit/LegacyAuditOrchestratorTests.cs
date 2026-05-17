using LegacyLens.Application.Audit;
using LegacyLens.Application.DotNetAnalysis;
using LegacyLens.Application.Review;
using LegacyLens.Application.Search;
using LegacyLens.Domain.Search;

namespace LegacyLens.Application.Tests.Audit;

public sealed class LegacyAuditOrchestratorTests : IDisposable
{
    private readonly string _root;

    public LegacyAuditOrchestratorTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"LegacyLensAuditTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); }
        catch { /* best effort */ }
    }

    [Fact]
    public async Task AuditAsync_SucceedsWithoutLlm()
    {
        var orchestrator = CreateOrchestrator(new NullGpuSearchClient(), new NullLlmProvider());

        var report = await orchestrator.AuditAsync(
            new LegacyAuditRequest(_root, UseLlm: false, IncludeRoslyn: false, IncludeGpuSearch: false, IncludeDependencyInjection: false),
            CancellationToken.None);

        Assert.NotNull(report);
        Assert.Equal(_root, report.RepoPath);
        Assert.NotEmpty(report.Markdown);
    }

    [Fact]
    public async Task AuditAsync_DetectsWebConfig()
    {
        await File.WriteAllTextAsync(Path.Combine(_root, "web.config"), "<configuration/>");
        var orchestrator = CreateOrchestrator(new NullGpuSearchClient(), new NullLlmProvider());

        var report = await orchestrator.AuditAsync(
            new LegacyAuditRequest(_root, IncludeRoslyn: false, IncludeGpuSearch: false, IncludeDependencyInjection: false),
            CancellationToken.None);

        Assert.Contains(report.TechnologySignals, s => s.Name.Contains("web.config"));
        Assert.Contains(report.RiskFindings, f => f.Code == "web-config-present");
    }

    [Fact]
    public async Task AuditAsync_DetectsPackagesConfig()
    {
        await File.WriteAllTextAsync(Path.Combine(_root, "packages.config"), "<packages/>");
        var orchestrator = CreateOrchestrator(new NullGpuSearchClient(), new NullLlmProvider());

        var report = await orchestrator.AuditAsync(
            new LegacyAuditRequest(_root, IncludeRoslyn: false, IncludeGpuSearch: false, IncludeDependencyInjection: false),
            CancellationToken.None);

        Assert.Contains(report.TechnologySignals, s => s.Name.Contains("packages.config"));
        Assert.Contains(report.RiskFindings, f => f.Code == "packages-config-present");
    }

    [Fact]
    public async Task AuditAsync_DetectsGlobalAsax()
    {
        await File.WriteAllTextAsync(Path.Combine(_root, "Global.asax"), "<%@ Application %>");
        var orchestrator = CreateOrchestrator(new NullGpuSearchClient(), new NullLlmProvider());

        var report = await orchestrator.AuditAsync(
            new LegacyAuditRequest(_root, IncludeRoslyn: false, IncludeGpuSearch: false, IncludeDependencyInjection: false),
            CancellationToken.None);

        Assert.Contains(report.TechnologySignals, s => s.Name.Contains("Global.asax"));
    }

    [Fact]
    public async Task AuditAsync_AddsNoTestsFinding_WhenNoTestsPresent()
    {
        var orchestrator = CreateOrchestrator(new NullGpuSearchClient(), new NullLlmProvider());

        var report = await orchestrator.AuditAsync(
            new LegacyAuditRequest(_root, IncludeRoslyn: false, IncludeGpuSearch: false, IncludeDependencyInjection: false),
            CancellationToken.None);

        Assert.Contains(report.RiskFindings, f => f.Code == "no-tests-detected");
    }

    [Fact]
    public async Task AuditAsync_DoesNotAddNoTestsFinding_WhenTestsPresent()
    {
        var testDir = Directory.CreateDirectory(Path.Combine(_root, "MyApp.Tests"));
        await File.WriteAllTextAsync(Path.Combine(testDir.FullName, "ServiceTests.cs"), "// test");
        var orchestrator = CreateOrchestrator(new NullGpuSearchClient(), new NullLlmProvider());

        var report = await orchestrator.AuditAsync(
            new LegacyAuditRequest(_root, IncludeRoslyn: false, IncludeGpuSearch: false, IncludeDependencyInjection: false),
            CancellationToken.None);

        Assert.DoesNotContain(report.RiskFindings, f => f.Code == "no-tests-detected");
    }

    [Fact]
    public async Task AuditAsync_HandlesGpuSearchUnavailable_Gracefully()
    {
        var orchestrator = CreateOrchestrator(new FailingGpuSearchClient(), new NullLlmProvider());

        var report = await orchestrator.AuditAsync(
            new LegacyAuditRequest(_root, IncludeRoslyn: false, IncludeGpuSearch: true, IncludeDependencyInjection: false),
            CancellationToken.None);

        Assert.NotNull(report);
        Assert.Contains(report.RiskFindings, f => f.Code == "gpu-search-unavailable");
        Assert.NotNull(report.GpuSearchSummary);
        Assert.False(report.GpuSearchSummary!.WasAvailable);
    }

    [Fact]
    public async Task AuditAsync_HandlesRoslynLoadFailure_Gracefully()
    {
        var orchestrator = CreateOrchestrator(new NullGpuSearchClient(), new NullLlmProvider());

        var report = await orchestrator.AuditAsync(
            new LegacyAuditRequest(_root, IncludeRoslyn: true, IncludeGpuSearch: false, IncludeDependencyInjection: false),
            CancellationToken.None);

        Assert.NotNull(report);
        Assert.NotNull(report.RoslynSummary);
        Assert.False(report.RoslynSummary!.WorkspaceLoaded);
        Assert.Contains(report.RiskFindings, f => f.Code == "roslyn-unavailable");
    }

    [Fact]
    public async Task AuditAsync_HandlesDiAnalysisFailure_Gracefully()
    {
        var orchestrator = CreateOrchestrator(
            new NullGpuSearchClient(),
            new NullLlmProvider(),
            diAnalyzer: new FailingDiAnalyzer());

        var report = await orchestrator.AuditAsync(
            new LegacyAuditRequest(_root, IncludeRoslyn: false, IncludeGpuSearch: false, IncludeDependencyInjection: true),
            CancellationToken.None);

        Assert.NotNull(report);
        Assert.Contains(report.RiskFindings, f => f.Code == "di-analysis-failed");
    }

    [Fact]
    public async Task AuditAsync_IncludesDiFindings_WhenAvailable()
    {
        var orchestrator = CreateOrchestrator(
            new NullGpuSearchClient(),
            new NullLlmProvider(),
            diAnalyzer: new StubDiAnalyzer());

        var report = await orchestrator.AuditAsync(
            new LegacyAuditRequest(_root, IncludeRoslyn: false, IncludeGpuSearch: false, IncludeDependencyInjection: true),
            CancellationToken.None);

        Assert.NotNull(report.DependencyInjectionSummary);
        Assert.Equal(2, report.DependencyInjectionSummary!.RegistrationCount);
        Assert.Contains(report.RiskFindings, f => f.Code == "multiple-registrations");
    }

    [Fact]
    public async Task AuditAsync_LlmNotCalled_WhenUseLlmFalse()
    {
        var llmProvider = new TrackingLlmProvider();
        var orchestrator = CreateOrchestrator(new NullGpuSearchClient(), llmProvider);

        await orchestrator.AuditAsync(
            new LegacyAuditRequest(_root, UseLlm: false),
            CancellationToken.None);

        Assert.False(llmProvider.WasCalled);
    }

    [Fact]
    public async Task AuditAsync_IncludesLlmSummary_WhenUseLlmTrue()
    {
        var orchestrator = CreateOrchestrator(new NullGpuSearchClient(), new SucceedingLlmProvider());

        var report = await orchestrator.AuditAsync(
            new LegacyAuditRequest(_root, UseLlm: true, IncludeRoslyn: false, IncludeGpuSearch: false, IncludeDependencyInjection: false),
            CancellationToken.None);

        Assert.Equal("Audit summary.", report.LlmSummary);
    }

    [Fact]
    public async Task AuditAsync_LlmFailureDoesNotFailAudit()
    {
        var orchestrator = CreateOrchestrator(new NullGpuSearchClient(), new FailingLlmProvider());

        var report = await orchestrator.AuditAsync(
            new LegacyAuditRequest(_root, UseLlm: true, IncludeRoslyn: false, IncludeGpuSearch: false, IncludeDependencyInjection: false),
            CancellationToken.None);

        Assert.NotNull(report);
        Assert.Null(report.LlmSummary);
    }

    [Fact]
    public async Task AuditAsync_MarkdownContainsRequiredSections()
    {
        var orchestrator = CreateOrchestrator(new NullGpuSearchClient(), new NullLlmProvider());

        var report = await orchestrator.AuditAsync(
            new LegacyAuditRequest(_root, IncludeRoslyn: false, IncludeGpuSearch: false, IncludeDependencyInjection: false),
            CancellationToken.None);

        Assert.Contains("# Legacy .NET Audit Report", report.Markdown);
        Assert.Contains("## Summary", report.Markdown);
        Assert.Contains("## Risk Findings", report.Markdown);
        Assert.Contains("## Recommended Next Steps", report.Markdown);
        Assert.Contains("## Limitations", report.Markdown);
    }

    [Fact]
    public async Task AuditAsync_GeneratesRecommendedNextSteps()
    {
        await File.WriteAllTextAsync(Path.Combine(_root, "web.config"), "<configuration/>");
        var orchestrator = CreateOrchestrator(new NullGpuSearchClient(), new NullLlmProvider());

        var report = await orchestrator.AuditAsync(
            new LegacyAuditRequest(_root, IncludeRoslyn: false, IncludeGpuSearch: false, IncludeDependencyInjection: false),
            CancellationToken.None);

        Assert.NotEmpty(report.RecommendedNextSteps);
    }

    [Fact]
    public async Task AuditAsync_UsesSignalScan_WhenAvailable()
    {
        var gpuClient = new SignalScanGpuSearchClient();
        var orchestrator = CreateOrchestrator(gpuClient, new NullLlmProvider());

        var report = await orchestrator.AuditAsync(
            new LegacyAuditRequest(_root, IncludeRoslyn: false, IncludeGpuSearch: true, IncludeDependencyInjection: false),
            CancellationToken.None);

        Assert.NotNull(report.GpuSearchSummary);
        Assert.True(report.GpuSearchSummary.WasAvailable);
        Assert.True(report.GpuSearchSummary.UsedSignalScan);
        Assert.Equal(2, report.GpuSearchSummary.QueriesRun);
        Assert.Contains(report.GpuSearchSummary.Results, r => r.Query == "System.Web");
    }

    [Fact]
    public async Task AuditAsync_FallsBackToIndividualQueries_WhenSignalScanReturns404()
    {
        var orchestrator = CreateOrchestrator(new NullGpuSearchClient(), new NullLlmProvider());

        var report = await orchestrator.AuditAsync(
            new LegacyAuditRequest(_root, IncludeRoslyn: false, IncludeGpuSearch: true, IncludeDependencyInjection: false),
            CancellationToken.None);

        Assert.NotNull(report.GpuSearchSummary);
        Assert.True(report.GpuSearchSummary.WasAvailable);
        Assert.False(report.GpuSearchSummary.UsedSignalScan);
        Assert.Contains(report.RiskFindings, f => f.Code == "gpu-search-scan-fallback");
    }

    [Fact]
    public async Task AuditAsync_SignalScanResultsMappedToTechnologySignals()
    {
        var gpuClient = new SignalScanGpuSearchClient();
        var orchestrator = CreateOrchestrator(gpuClient, new NullLlmProvider());

        var report = await orchestrator.AuditAsync(
            new LegacyAuditRequest(_root, IncludeRoslyn: false, IncludeGpuSearch: true, IncludeDependencyInjection: false),
            CancellationToken.None);

        Assert.Contains(report.TechnologySignals, s => s.Name == ".NET Framework / System.Web");
        Assert.Contains(report.RiskFindings, f => f.Code == "legacy-framework-detected");
    }

    [Fact]
    public async Task AuditAsync_SignalScanMarkdownContainsSignalScanSection()
    {
        var gpuClient = new SignalScanGpuSearchClient();
        var orchestrator = CreateOrchestrator(gpuClient, new NullLlmProvider());

        var report = await orchestrator.AuditAsync(
            new LegacyAuditRequest(_root, IncludeRoslyn: false, IncludeGpuSearch: true, IncludeDependencyInjection: false),
            CancellationToken.None);

        Assert.Contains("## gpu-search Signal Scan", report.Markdown);
        Assert.Contains("Signal scan", report.Markdown);
    }

    [Fact]
    public async Task AuditAsync_InvokesProvidersAndMergesResults()
    {
        var provider = new TrackingAuditProvider();
        var orchestrator = new LegacyAuditOrchestrator(
            new StubWorkspaceDiscovery(),
            [provider],
            new NullLlmProvider(),
            new LegacyAuditMarkdownFormatter());

        var report = await orchestrator.AuditAsync(
            new LegacyAuditRequest(_root, IncludeRoslyn: false, IncludeGpuSearch: false, IncludeDependencyInjection: false),
            CancellationToken.None);

        Assert.True(provider.WasCalled);
        Assert.Contains(report.TechnologySignals, s => s.Name == "Provider tech");
        Assert.Contains(report.ArchitectureSignals, s => s.Name == "Provider architecture");
        Assert.Contains(report.RiskFindings, f => f.Code == "provider-risk");
        Assert.Contains("Review provider output.", report.RecommendedNextSteps);
    }

    [Fact]
    public async Task AuditAsync_ProviderFailureAddsFindingAndContinues()
    {
        var orchestrator = new LegacyAuditOrchestrator(
            new StubWorkspaceDiscovery(),
            [new ThrowingAuditProvider(), new TrackingAuditProvider()],
            new NullLlmProvider(),
            new LegacyAuditMarkdownFormatter());

        var report = await orchestrator.AuditAsync(
            new LegacyAuditRequest(_root, IncludeRoslyn: false, IncludeGpuSearch: false, IncludeDependencyInjection: false),
            CancellationToken.None);

        Assert.Contains(report.RiskFindings, f => f.Code == "audit-provider-failed");
        Assert.Contains(report.RiskFindings, f => f.Code == "provider-risk");
    }

    private LegacyAuditOrchestrator CreateOrchestrator(
        IGpuSearchClient gpuSearchClient,
        IReviewLlmProvider llmProvider,
        IRoslynDependencyInjectionAnalyzer? diAnalyzer = null)
    {
        return new LegacyAuditOrchestrator(
            new StubWorkspaceDiscovery(),
            [
                new TechnologySignalAuditProvider(),
                new RoslynAuditProvider(new StubSymbolScanner()),
                new DependencyInjectionAuditProvider(diAnalyzer ?? new NullDiAnalyzer()),
                new GpuSearchSignalAuditProvider(gpuSearchClient),
                new ArchitectureSignalAuditProvider(),
                new RecommendedNextStepsAuditProvider()
            ],
            llmProvider,
            new LegacyAuditMarkdownFormatter());
    }

    private sealed class TrackingAuditProvider : IAuditProvider
    {
        public string Name => "Tracking";
        public bool WasCalled { get; private set; }

        public Task<AuditProviderResult> AnalyzeAsync(AuditContext context, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(new AuditProviderResult(
                Name,
                TechnologySignals: [new TechnologySignal("Provider tech", "Test", "provider", null, "High")],
                ArchitectureSignals: [new ArchitectureSignal("Provider architecture", "Provider architecture signal.", "provider", "Medium")],
                RiskFindings: [new AuditFinding("Medium", "provider-risk", "Provider risk", "Provider risk finding.")],
                RecommendedNextSteps: ["Review provider output."]));
        }
    }

    private sealed class ThrowingAuditProvider : IAuditProvider
    {
        public string Name => "Throwing";

        public Task<AuditProviderResult> AnalyzeAsync(AuditContext context, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Provider failed.");
    }

    private sealed class StubWorkspaceDiscovery : IDotNetWorkspaceDiscovery
    {
        public Task<DotNetWorkspaceDiscoveryResult> DiscoverAsync(string repoRoot, CancellationToken cancellationToken)
            => Task.FromResult(new DotNetWorkspaceDiscoveryResult([], null, []));
    }

    private sealed class StubWorkspaceLoader : IRoslynWorkspaceLoader
    {
        public Task<RoslynWorkspaceLoadResult> LoadAsync(DotNetWorkspaceDiscoveryResult discoveryResult, CancellationToken cancellationToken)
            => Task.FromResult(new RoslynWorkspaceLoadResult(false, null, null, 0, 0, [], [], "No workspace."));
    }

    private sealed class StubSymbolScanner : IRoslynSymbolScanner
    {
        public Task<DotNetWorkspaceScanResponse> ScanAsync(string repoRoot, CancellationToken cancellationToken)
            => Task.FromResult(new DotNetWorkspaceScanResponse(
                null, [], 0, 0, 0, [],
                new RoslynWorkspaceLoadResult(false, null, null, 0, 0, [], [], null)));
    }

    private sealed class NullDiAnalyzer : IRoslynDependencyInjectionAnalyzer
    {
        public Task<DependencyInjectionAnalysisResult> AnalyzeAsync(string repoPath, CancellationToken cancellationToken)
            => Task.FromResult(new DependencyInjectionAnalysisResult(true, null, null, [], [], [], [], null));
    }

    private sealed class FailingDiAnalyzer : IRoslynDependencyInjectionAnalyzer
    {
        public Task<DependencyInjectionAnalysisResult> AnalyzeAsync(string repoPath, CancellationToken cancellationToken)
            => throw new InvalidOperationException("DI analysis failed.");
    }

    private sealed class StubDiAnalyzer : IRoslynDependencyInjectionAnalyzer
    {
        public Task<DependencyInjectionAnalysisResult> AnalyzeAsync(string repoPath, CancellationToken cancellationToken)
        {
            var registrations = new[]
            {
                new ServiceRegistrationInfo("IFoo", "Foo", "Scoped", "Startup.cs", 10, 1, "App", "services.AddScoped<IFoo, Foo>()"),
                new ServiceRegistrationInfo("IFoo", "AltFoo", "Scoped", "Startup.cs", 15, 1, "App", "services.AddScoped<IFoo, AltFoo>()")
            };
            var findings = new[]
            {
                new DependencyInjectionFinding("Warning", "multiple-registrations", "IFoo is registered 2 times.", "Startup.cs", 10, 1)
            };
            return Task.FromResult(new DependencyInjectionAnalysisResult(
                true, null, null, registrations, [], findings, [], null));
        }
    }

    private sealed class SignalScanGpuSearchClient : IGpuSearchClient
    {
        public Task<GpuSearchHealth> GetHealthAsync(CancellationToken cancellationToken) => Task.FromResult(new GpuSearchHealth("ok"));
        public Task<GpuSearchStats> GetStatsAsync(CancellationToken cancellationToken) => Task.FromResult(new GpuSearchStats("ok", null, null, null));
        public Task<IReadOnlyList<GpuSearchResult>> SearchCodeAsync(CodeSearchRequest request, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<GpuSearchResult>>([]);
        public Task<IReadOnlyList<GpuSearchResult>> SearchSemanticAsync(CodeSearchRequest request, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<GpuSearchResult>>([]);
        public Task<IReadOnlyList<GpuSearchResult>> SearchHybridAsync(SearchHybridRequest request, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<GpuSearchResult>>([]);
        public Task<ReadBlockResponse> ReadBlockAsync(ReadBlockRequest request, CancellationToken cancellationToken) => Task.FromResult(new ReadBlockResponse("ok", string.Empty, null, null, null, null, null));
        public Task<ReadSkeletonResponse> ReadSkeletonAsync(ReadSkeletonRequest request, CancellationToken cancellationToken) => Task.FromResult(new ReadSkeletonResponse("ok", string.Empty, null, null, null, null));
        public Task<DependencyImpactResponse> GetDependencyImpactAsync(DependencyImpactRequest request, CancellationToken cancellationToken) => Task.FromResult(new DependencyImpactResponse("ok", string.Empty, null, []));
        public Task<GpuSearchIndexRootResponse> IndexRootAsync(GpuSearchIndexRootRequest request, CancellationToken cancellationToken) => Task.FromResult(new GpuSearchIndexRootResponse(true, request.Directory, request.Directory, true, true, null, null, null, null));

        public Task<SignalScanResponse> ScanSignalsAsync(SignalScanRequest request, CancellationToken cancellationToken)
        {
            var signals = new List<RepositorySignal>
            {
                new("framework-system-web", "Framework", "System.Web", "Legacy System.Web reference", "high", "System.Web",
                    [new SignalMatch("src/Startup.cs", null, 1, null, 0.9, null, "using System.Web;", "hybrid")]),
                new("quality-sync-over-async", "Quality", ".Result usage", "Sync-over-async pattern", "medium", ".Result",
                    [new SignalMatch("src/Service.cs", null, 5, null, 0.8, null, "var x = task.Result;", "hybrid")])
            };
            var summary = new SignalScanSummary(signals.Count, 2, new Dictionary<string, int> { ["Framework"] = 1, ["Quality"] = 1 });
            var response = new SignalScanResponse("ok", ["Framework", "Quality"], summary, signals, null, null);
            return Task.FromResult(response);
        }
    }

    private sealed class NullGpuSearchClient : IGpuSearchClient
    {
        public Task<GpuSearchHealth> GetHealthAsync(CancellationToken cancellationToken) => Task.FromResult(new GpuSearchHealth("ok"));
        public Task<GpuSearchStats> GetStatsAsync(CancellationToken cancellationToken) => Task.FromResult(new GpuSearchStats("ok", null, null, null));
        public Task<IReadOnlyList<GpuSearchResult>> SearchCodeAsync(CodeSearchRequest request, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<GpuSearchResult>>([]);
        public Task<IReadOnlyList<GpuSearchResult>> SearchSemanticAsync(CodeSearchRequest request, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<GpuSearchResult>>([]);
        public Task<IReadOnlyList<GpuSearchResult>> SearchHybridAsync(SearchHybridRequest request, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<GpuSearchResult>>([]);
        public Task<ReadBlockResponse> ReadBlockAsync(ReadBlockRequest request, CancellationToken cancellationToken) => Task.FromResult(new ReadBlockResponse("ok", string.Empty, null, null, null, null, null));
        public Task<ReadSkeletonResponse> ReadSkeletonAsync(ReadSkeletonRequest request, CancellationToken cancellationToken) => Task.FromResult(new ReadSkeletonResponse("ok", string.Empty, null, null, null, null));
        public Task<DependencyImpactResponse> GetDependencyImpactAsync(DependencyImpactRequest request, CancellationToken cancellationToken) => Task.FromResult(new DependencyImpactResponse("ok", string.Empty, null, []));
        public Task<GpuSearchIndexRootResponse> IndexRootAsync(GpuSearchIndexRootRequest request, CancellationToken cancellationToken) => Task.FromResult(new GpuSearchIndexRootResponse(true, request.Directory, request.Directory, true, true, null, null, null, null));
        public Task<SignalScanResponse> ScanSignalsAsync(SignalScanRequest request, CancellationToken cancellationToken) => throw new HttpRequestException("Not found", null, System.Net.HttpStatusCode.NotFound);
    }

    private sealed class FailingGpuSearchClient : IGpuSearchClient
    {
        public Task<GpuSearchHealth> GetHealthAsync(CancellationToken cancellationToken) => throw new HttpRequestException("Connection refused");
        public Task<GpuSearchStats> GetStatsAsync(CancellationToken cancellationToken) => throw new HttpRequestException("Connection refused");
        public Task<IReadOnlyList<GpuSearchResult>> SearchCodeAsync(CodeSearchRequest request, CancellationToken cancellationToken) => throw new HttpRequestException("Connection refused");
        public Task<IReadOnlyList<GpuSearchResult>> SearchSemanticAsync(CodeSearchRequest request, CancellationToken cancellationToken) => throw new HttpRequestException("Connection refused");
        public Task<IReadOnlyList<GpuSearchResult>> SearchHybridAsync(SearchHybridRequest request, CancellationToken cancellationToken) => throw new HttpRequestException("Connection refused");
        public Task<ReadBlockResponse> ReadBlockAsync(ReadBlockRequest request, CancellationToken cancellationToken) => throw new HttpRequestException("Connection refused");
        public Task<ReadSkeletonResponse> ReadSkeletonAsync(ReadSkeletonRequest request, CancellationToken cancellationToken) => throw new HttpRequestException("Connection refused");
        public Task<DependencyImpactResponse> GetDependencyImpactAsync(DependencyImpactRequest request, CancellationToken cancellationToken) => throw new HttpRequestException("Connection refused");
        public Task<GpuSearchIndexRootResponse> IndexRootAsync(GpuSearchIndexRootRequest request, CancellationToken cancellationToken) => throw new HttpRequestException("Connection refused");
        public Task<SignalScanResponse> ScanSignalsAsync(SignalScanRequest request, CancellationToken cancellationToken) => throw new HttpRequestException("Connection refused");
    }

    private sealed class NullLlmProvider : IReviewLlmProvider
    {
        public string Name => "Null";
        public Task<string?> GenerateReviewAsync(LlmReviewPrompt prompt, CancellationToken cancellationToken) => Task.FromResult<string?>(null);
    }

    private sealed class SucceedingLlmProvider : IReviewLlmProvider
    {
        public string Name => "Test";
        public Task<string?> GenerateReviewAsync(LlmReviewPrompt prompt, CancellationToken cancellationToken) => Task.FromResult<string?>("Audit summary.");
    }

    private sealed class FailingLlmProvider : IReviewLlmProvider
    {
        public string Name => "FailingLlm";
        public Task<string?> GenerateReviewAsync(LlmReviewPrompt prompt, CancellationToken cancellationToken) => throw new HttpRequestException("LLM unavailable");
    }

    private sealed class TrackingLlmProvider : IReviewLlmProvider
    {
        public bool WasCalled { get; private set; }
        public string Name => "Tracking";
        public Task<string?> GenerateReviewAsync(LlmReviewPrompt prompt, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult<string?>(null);
        }
    }
}
