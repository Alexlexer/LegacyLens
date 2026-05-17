using LegacyLens.Application.Audit;
using LegacyLens.Application.DotNetAnalysis;
using LegacyLens.Application.Git;
using LegacyLens.Application.Reports;
using LegacyLens.Application.Review;
using LegacyLens.Application.Search;
using LegacyLens.Domain.Git;
using LegacyLens.Domain.Search;

namespace LegacyLens.Application.Tests;

public sealed class DiffReviewOrchestratorTests
{
    [Fact]
    public async Task ReviewDiffAsync_ReturnsMarkdownReportWithFindings()
    {
        var diff = new GitDiffPreviewResponse(
            "repo",
            2,
            [
                new GitDiffFile("src/App.cs", "M", 5, 1),
                new GitDiffFile("tests/AppTests.cs", "M", 10, 0)
            ],
            "diff");
        var orchestrator = CreateOrchestrator(diff, new NullGpuSearchClient());

        var report = await orchestrator.ReviewDiffAsync(new DiffReviewRequest("repo"), CancellationToken.None);

        Assert.Equal("repo", report.RepoPath);
        Assert.Equal(2, report.ChangedFileCount);
        Assert.Contains(report.Findings, f => f.RuleId == "test-change");
        Assert.Contains("# LegacyLens Diff Review", report.Markdown);
    }

    [Fact]
    public async Task ReviewDiffAsync_AddsEmptyDiffFinding_WhenNoFilesChanged()
    {
        var diff = new GitDiffPreviewResponse("repo", 0, [], string.Empty);
        var orchestrator = CreateOrchestrator(diff, new NullGpuSearchClient());

        var report = await orchestrator.ReviewDiffAsync(new DiffReviewRequest("repo"), CancellationToken.None);

        Assert.Contains(report.Findings, f => f.RuleId == "empty-diff");
    }

    [Fact]
    public async Task ReviewDiffAsync_SavesReport()
    {
        var repository = new StubReportRepository();
        var diff = new GitDiffPreviewResponse("repo", 0, [], string.Empty);
        var orchestrator = CreateOrchestrator(diff, new NullGpuSearchClient(), repository);

        var report = await orchestrator.ReviewDiffAsync(new DiffReviewRequest("repo"), CancellationToken.None);

        Assert.Same(report, repository.SavedReport);
    }

    [Fact]
    public async Task ReviewDiffAsync_AddsGpuSearchUnavailableFinding_WhenGpuSearchFails()
    {
        var diff = new GitDiffPreviewResponse(
            "repo",
            1,
            [new GitDiffFile("src/App.cs", "M", 2, 1)],
            "diff");
        var orchestrator = CreateOrchestrator(diff, new FailingGpuSearchClient());

        var report = await orchestrator.ReviewDiffAsync(new DiffReviewRequest("repo"), CancellationToken.None);

        Assert.Contains(report.Findings, f => f.RuleId == "gpu-search-unavailable");
        Assert.NotNull(report.GpuSearchContext);
        Assert.False(report.GpuSearchContext!.WasAvailable);
        Assert.Contains("gpu-search Context", report.Markdown);
    }

    [Fact]
    public async Task ReviewDiffAsync_IncludesDependencyImpactFinding_WhenManyImporters()
    {
        var diff = new GitDiffPreviewResponse(
            "repo",
            1,
            [new GitDiffFile("src/UserService.cs", "M", 10, 2)],
            "diff");
        var gpuSearchClient = new RichGpuSearchClient(
            impactedFiles:
            [
                new ImpactedFile("src/AuthController.cs", null, 1),
                new ImpactedFile("src/AdminController.cs", null, 1),
                new ImpactedFile("src/RootController.cs", null, 2),
                new ImpactedFile("src/Api.cs", null, 2)
            ]);
        var orchestrator = CreateOrchestrator(diff, gpuSearchClient);

        var report = await orchestrator.ReviewDiffAsync(new DiffReviewRequest("repo"), CancellationToken.None);

        Assert.Contains(report.Findings, f => f.RuleId == "high-impact-change");
        Assert.NotNull(report.GpuSearchContext);
        Assert.True(report.GpuSearchContext!.WasAvailable);
        Assert.Single(report.GpuSearchContext.Files);
        Assert.Equal("src/UserService.cs", report.GpuSearchContext.Files[0].FilePath);
    }

    [Fact]
    public async Task ReviewDiffAsync_IncludesGpuSearchContextInMarkdown()
    {
        var diff = new GitDiffPreviewResponse(
            "repo",
            1,
            [new GitDiffFile("src/UserService.cs", "M", 10, 2)],
            "diff");
        var gpuSearchClient = new RichGpuSearchClient(
            impactedFiles:
            [
                new ImpactedFile("src/AuthController.cs", null, 1)
            ],
            skeletonContent: "public class UserService { }");
        var orchestrator = CreateOrchestrator(diff, gpuSearchClient);

        var report = await orchestrator.ReviewDiffAsync(new DiffReviewRequest("repo"), CancellationToken.None);

        Assert.Contains("gpu-search Context", report.Markdown);
        Assert.Contains("src/UserService.cs", report.Markdown);
    }

    [Fact]
    public async Task ReviewDiffAsync_StillReturnsReport_WhenIndividualGpuCallFails()
    {
        var diff = new GitDiffPreviewResponse(
            "repo",
            1,
            [new GitDiffFile("src/App.cs", "M", 2, 1)],
            "diff");
        var gpuSearchClient = new PartiallyFailingGpuSearchClient();
        var orchestrator = CreateOrchestrator(diff, gpuSearchClient);

        var report = await orchestrator.ReviewDiffAsync(new DiffReviewRequest("repo"), CancellationToken.None);

        Assert.NotNull(report);
        Assert.NotNull(report.GpuSearchContext);
        Assert.True(report.GpuSearchContext!.WasAvailable);
        Assert.Single(report.GpuSearchContext.Files);
        Assert.NotNull(report.GpuSearchContext.Files[0].Error);
    }

    [Fact]
    public async Task ReviewDiffAsync_RespectsMaxFilesToEnrich()
    {
        var diff = new GitDiffPreviewResponse(
            "repo",
            3,
            [
                new GitDiffFile("src/One.cs", "M", 1, 0),
                new GitDiffFile("src/Two.cs", "M", 1, 0),
                new GitDiffFile("src/Three.cs", "M", 1, 0)
            ],
            "diff");
        var orchestrator = CreateOrchestrator(
            diff,
            new RichGpuSearchClient(),
            enrichmentOptions: new ReviewEnrichmentOptions { MaxFilesToEnrich = 2 });

        var report = await orchestrator.ReviewDiffAsync(new DiffReviewRequest("repo"), CancellationToken.None);

        Assert.NotNull(report.GpuSearchContext);
        Assert.Equal(2, report.GpuSearchContext!.Files.Count);
        Assert.Equal(["src/One.cs", "src/Two.cs"], report.GpuSearchContext.Files.Select(f => f.FilePath).ToArray());
    }

    [Fact]
    public async Task ReviewDiffAsync_RespectsMaxSearchResultsPerFile()
    {
        var diff = new GitDiffPreviewResponse(
            "repo",
            1,
            [new GitDiffFile("src/UserService.cs", "M", 1, 0)],
            "diff");
        var gpuSearchClient = new RichGpuSearchClient();
        var orchestrator = CreateOrchestrator(
            diff,
            gpuSearchClient,
            enrichmentOptions: new ReviewEnrichmentOptions { MaxSearchResultsPerFile = 3 });

        await orchestrator.ReviewDiffAsync(new DiffReviewRequest("repo"), CancellationToken.None);

        Assert.Equal(3, gpuSearchClient.LastSearchHybridLimit);
    }

    [Fact]
    public async Task ReviewDiffAsync_IncludesRoslynContext_WhenCsFilesChanged()
    {
        var diff = new GitDiffPreviewResponse(
            "repo",
            1,
            [new GitDiffFile("src/UserService.cs", "M", 10, 2)],
            "diff");
        var orchestrator = CreateOrchestrator(
            diff,
            new NullGpuSearchClient(),
            roslynReferenceAnalyzer: new SucceedingRoslynReferenceAnalyzer());

        var report = await orchestrator.ReviewDiffAsync(new DiffReviewRequest("repo"), CancellationToken.None);

        Assert.NotNull(report.RoslynContext);
        Assert.True(report.RoslynContext!.Success);
        Assert.Single(report.RoslynContext.ChangedSymbols);
        Assert.Equal("UserService", report.RoslynContext.ChangedSymbols[0].Name);
        Assert.Contains("Roslyn Reference Context", report.Markdown);
    }

    [Fact]
    public async Task ReviewDiffAsync_SkipsRoslynEnrichment_WhenNoCsFilesChanged()
    {
        var diff = new GitDiffPreviewResponse(
            "repo",
            1,
            [new GitDiffFile("appsettings.json", "M", 2, 1)],
            "diff");
        var orchestrator = CreateOrchestrator(diff, new NullGpuSearchClient());

        var report = await orchestrator.ReviewDiffAsync(new DiffReviewRequest("repo"), CancellationToken.None);

        Assert.Null(report.RoslynContext);
        Assert.DoesNotContain("Roslyn Reference Context", report.Markdown);
    }

    [Fact]
    public async Task ReviewDiffAsync_AddsRoslynUnavailableFinding_WhenRoslynFails()
    {
        var diff = new GitDiffPreviewResponse(
            "repo",
            1,
            [new GitDiffFile("src/App.cs", "M", 5, 1)],
            "diff");
        var orchestrator = CreateOrchestrator(
            diff,
            new NullGpuSearchClient(),
            roslynReferenceAnalyzer: new NullRoslynReferenceAnalyzer());

        var report = await orchestrator.ReviewDiffAsync(new DiffReviewRequest("repo"), CancellationToken.None);

        Assert.Contains(report.Findings, f => f.RuleId == "roslyn-unavailable");
        Assert.NotNull(report.RoslynContext);
        Assert.False(report.RoslynContext!.Success);
    }

    [Fact]
    public async Task ReviewDiffAsync_RespectsMaxSymbolsForReferenceAnalysis()
    {
        var diff = new GitDiffPreviewResponse(
            "repo",
            3,
            [
                new GitDiffFile("src/One.cs", "M", 1, 0),
                new GitDiffFile("src/Two.cs", "M", 1, 0),
                new GitDiffFile("src/Three.cs", "M", 1, 0)
            ],
            "diff");
        var analyzer = new CountingRoslynReferenceAnalyzer();
        var orchestrator = CreateOrchestrator(
            diff,
            new NullGpuSearchClient(),
            enrichmentOptions: new ReviewEnrichmentOptions { MaxSymbolsForReferenceAnalysis = 2 },
            roslynReferenceAnalyzer: analyzer);

        await orchestrator.ReviewDiffAsync(new DiffReviewRequest("repo"), CancellationToken.None);

        Assert.Equal(2, analyzer.CallCount);
    }

    private static DiffReviewOrchestrator CreateOrchestrator(
        GitDiffPreviewResponse diff,
        IGpuSearchClient gpuSearchClient,
        IReportRepository? repository = null,
        ReviewEnrichmentOptions? enrichmentOptions = null,
        IRoslynReferenceAnalyzer? roslynReferenceAnalyzer = null)
    {
        return new DiffReviewOrchestrator(
            new StubGitDiffService(diff),
            gpuSearchClient,
            new MarkdownReviewReportFormatter(),
            new ReviewPromptBuilder(),
            new StubReviewLlmProvider(),
            repository ?? new StubReportRepository(),
            enrichmentOptions ?? new ReviewEnrichmentOptions(),
            roslynReferenceAnalyzer ?? new NullRoslynReferenceAnalyzer());
    }

    private sealed class StubGitDiffService(GitDiffPreviewResponse response) : IGitDiffService
    {
        public Task<GitDiffPreviewResponse> GetCurrentDiffAsync(
            GitDiffPreviewRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult(response);
    }

    private sealed class StubReviewLlmProvider : IReviewLlmProvider
    {
        public string Name => "Stub";

        public Task<string?> GenerateReviewAsync(LlmReviewPrompt prompt, CancellationToken cancellationToken)
            => Task.FromResult<string?>(null);
    }

    private sealed class StubReportRepository : IReportRepository
    {
        public DiffReviewReport? SavedReport { get; private set; }

        public Task SaveAsync(DiffReviewReport report, CancellationToken cancellationToken)
        {
            SavedReport = report;
            return Task.CompletedTask;
        }

        public Task<DiffReviewReport?> GetByIdAsync(string reportId, CancellationToken cancellationToken)
            => Task.FromResult<DiffReviewReport?>(SavedReport?.ReportId == reportId ? SavedReport : null);

        public Task<IReadOnlyList<ReportSummary>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ReportSummary>>([]);

        public Task<bool> DeleteAsync(string reportId, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task SaveAuditAsync(LegacyAuditReport report, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<LegacyAuditReport?> GetAuditByIdAsync(string reportId, CancellationToken cancellationToken)
            => Task.FromResult<LegacyAuditReport?>(null);
    }

    private sealed class NullGpuSearchClient : IGpuSearchClient
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
            => Task.FromResult<IReadOnlyList<GpuSearchResult>>([]);

        public Task<ReadBlockResponse> ReadBlockAsync(ReadBlockRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new ReadBlockResponse("ok", request.Path, null, null, null, null, null));

        public Task<ReadSkeletonResponse> ReadSkeletonAsync(ReadSkeletonRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new ReadSkeletonResponse("ok", request.Path, null, null, null, null));

        public Task<DependencyImpactResponse> GetDependencyImpactAsync(DependencyImpactRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new DependencyImpactResponse("ok", request.Path, null, []));

        public Task<GpuSearchIndexRootResponse> IndexRootAsync(GpuSearchIndexRootRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new GpuSearchIndexRootResponse(true, request.Directory, request.Directory, true, true, null, null, null, null));

        public Task<SignalScanResponse> ScanSignalsAsync(SignalScanRequest request, CancellationToken cancellationToken)
            => throw new HttpRequestException("Not found", null, System.Net.HttpStatusCode.NotFound);
    }

    private sealed class FailingGpuSearchClient : IGpuSearchClient
    {
        public Task<GpuSearchHealth> GetHealthAsync(CancellationToken cancellationToken)
            => throw new HttpRequestException("gpu-search-mcp is not running");

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

        public Task<GpuSearchIndexRootResponse> IndexRootAsync(GpuSearchIndexRootRequest request, CancellationToken cancellationToken)
            => throw new HttpRequestException("unavailable");

        public Task<SignalScanResponse> ScanSignalsAsync(SignalScanRequest request, CancellationToken cancellationToken)
            => throw new HttpRequestException("unavailable");
    }

    private sealed class RichGpuSearchClient(
        IReadOnlyList<ImpactedFile>? impactedFiles = null,
        string? skeletonContent = null) : IGpuSearchClient
    {
        public int? LastSearchHybridLimit { get; private set; }

        public Task<GpuSearchHealth> GetHealthAsync(CancellationToken cancellationToken)
            => Task.FromResult(new GpuSearchHealth("ok"));

        public Task<GpuSearchStats> GetStatsAsync(CancellationToken cancellationToken)
            => Task.FromResult(new GpuSearchStats("ok", "cuda", "RTX 4060", 50));

        public Task<IReadOnlyList<GpuSearchResult>> SearchCodeAsync(CodeSearchRequest request, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<GpuSearchResult>>([]);

        public Task<IReadOnlyList<GpuSearchResult>> SearchSemanticAsync(CodeSearchRequest request, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<GpuSearchResult>>([]);

        public Task<IReadOnlyList<GpuSearchResult>> SearchHybridAsync(SearchHybridRequest request, CancellationToken cancellationToken)
        {
            LastSearchHybridLimit = request.Limit;
            return Task.FromResult<IReadOnlyList<GpuSearchResult>>(
                [new GpuSearchResult("src/Related.cs", null, 10, 15, 0.8, null, "related code", "hybrid")]);
        }

        public Task<ReadBlockResponse> ReadBlockAsync(ReadBlockRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new ReadBlockResponse("ok", request.Path, null, null, null, null, null));

        public Task<ReadSkeletonResponse> ReadSkeletonAsync(ReadSkeletonRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new ReadSkeletonResponse("ok", request.Path, null, skeletonContent, null, "csharp"));

        public Task<DependencyImpactResponse> GetDependencyImpactAsync(DependencyImpactRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new DependencyImpactResponse("ok", request.Path, null, impactedFiles ?? []));

        public Task<GpuSearchIndexRootResponse> IndexRootAsync(GpuSearchIndexRootRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new GpuSearchIndexRootResponse(true, request.Directory, request.Directory, true, true, null, null, null, null));

        public Task<SignalScanResponse> ScanSignalsAsync(SignalScanRequest request, CancellationToken cancellationToken)
            => throw new HttpRequestException("Not found", null, System.Net.HttpStatusCode.NotFound);
    }

    private sealed class NullRoslynReferenceAnalyzer : IRoslynReferenceAnalyzer
    {
        public Task<RoslynReferenceAnalysisResult> FindReferencesAsync(
            RoslynReferenceAnalysisRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult(new RoslynReferenceAnalysisResult(
                false, null, null, request.SymbolName, [], [], [], "Roslyn unavailable in tests."));
    }

    private sealed class SucceedingRoslynReferenceAnalyzer : IRoslynReferenceAnalyzer
    {
        public Task<RoslynReferenceAnalysisResult> FindReferencesAsync(
            RoslynReferenceAnalysisRequest request,
            CancellationToken cancellationToken)
        {
            var matchedSymbol = new RoslynMatchedSymbol(
                request.SymbolName,
                $"SampleApp.{request.SymbolName}",
                "class",
                $"src/{request.SymbolName}.cs",
                1,
                1,
                "SampleApp");
            var reference = new RoslynReferenceInfo(
                request.SymbolName,
                $"SampleApp.{request.SymbolName}",
                "class",
                "src/OtherService.cs",
                10,
                5,
                "SampleApp",
                "SampleApp.OtherService",
                "Reference",
                false);
            return Task.FromResult(new RoslynReferenceAnalysisResult(
                true,
                "/workspace/SampleApp.sln",
                DotNetWorkspaceKind.Sln,
                request.SymbolName,
                [matchedSymbol],
                [reference],
                [],
                null));
        }
    }

    private sealed class CountingRoslynReferenceAnalyzer : IRoslynReferenceAnalyzer
    {
        public int CallCount { get; private set; }

        public Task<RoslynReferenceAnalysisResult> FindReferencesAsync(
            RoslynReferenceAnalysisRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new RoslynReferenceAnalysisResult(
                false, null, null, request.SymbolName, [], [], [], "workspace not available"));
        }
    }

    private sealed class PartiallyFailingGpuSearchClient : IGpuSearchClient
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
            => Task.FromResult<IReadOnlyList<GpuSearchResult>>([]);

        public Task<ReadBlockResponse> ReadBlockAsync(ReadBlockRequest request, CancellationToken cancellationToken)
            => throw new HttpRequestException("block read failed");

        public Task<ReadSkeletonResponse> ReadSkeletonAsync(ReadSkeletonRequest request, CancellationToken cancellationToken)
            => throw new HttpRequestException("skeleton read failed");

        public Task<DependencyImpactResponse> GetDependencyImpactAsync(DependencyImpactRequest request, CancellationToken cancellationToken)
            => throw new HttpRequestException("impact read failed");

        public Task<GpuSearchIndexRootResponse> IndexRootAsync(GpuSearchIndexRootRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new GpuSearchIndexRootResponse(true, request.Directory, request.Directory, true, true, null, null, null, null));

        public Task<SignalScanResponse> ScanSignalsAsync(SignalScanRequest request, CancellationToken cancellationToken)
            => throw new HttpRequestException("Not found", null, System.Net.HttpStatusCode.NotFound);
    }
}
