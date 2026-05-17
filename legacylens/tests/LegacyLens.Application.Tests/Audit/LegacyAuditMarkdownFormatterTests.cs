using LegacyLens.Application.Audit;
using LegacyLens.Application.DotNetAnalysis;

namespace LegacyLens.Application.Tests.Audit;

public sealed class LegacyAuditMarkdownFormatterTests
{
    [Fact]
    public void Format_ContainsRequiredSections()
    {
        var report = BuildMinimalReport();

        var markdown = new LegacyAuditMarkdownFormatter().Format(report);

        Assert.Contains("# Legacy .NET Audit Report", markdown);
        Assert.Contains("## Summary", markdown);
        Assert.Contains("## Workspace", markdown);
        Assert.Contains("## Technology Signals", markdown);
        Assert.Contains("## Architecture Signals", markdown);
        Assert.Contains("## Risk Findings", markdown);
        Assert.Contains("## Roslyn Summary", markdown);
        Assert.Contains("## Dependency Injection Summary", markdown);
        Assert.Contains("## gpu-search Signal Scan", markdown);
        Assert.Contains("## Recommended Next Steps", markdown);
        Assert.Contains("## Limitations", markdown);
    }

    [Fact]
    public void Format_IncludesReportIdAndRepoPath()
    {
        var report = BuildMinimalReport() with { ReportId = "audit-test-1", RepoPath = "D:/Projects/Repo" };

        var markdown = new LegacyAuditMarkdownFormatter().Format(report);

        Assert.Contains("audit-test-1", markdown);
        Assert.Contains("D:/Projects/Repo", markdown);
    }

    [Fact]
    public void Format_IncludesTechnologySignals()
    {
        var report = BuildMinimalReport() with
        {
            TechnologySignals =
            [
                new TechnologySignal("ASP.NET MVC App_Start", "Framework", "App_Start found", "App_Start/", "high")
            ]
        };

        var markdown = new LegacyAuditMarkdownFormatter().Format(report);

        Assert.Contains("ASP.NET MVC App_Start", markdown);
        Assert.Contains("Framework", markdown);
        Assert.Contains("App_Start found", markdown);
    }

    [Fact]
    public void Format_IncludesRiskFindings()
    {
        var report = BuildMinimalReport() with
        {
            RiskFindings =
            [
                new AuditFinding("Warning", "web-config-present", "web.config present", "Legacy web.config found.", "web.config")
            ]
        };

        var markdown = new LegacyAuditMarkdownFormatter().Format(report);

        Assert.Contains("web-config-present", markdown);
        Assert.Contains("web.config present", markdown);
        Assert.Contains("Legacy web.config found.", markdown);
    }

    [Fact]
    public void Format_IncludesRoslynSummary_WhenLoaded()
    {
        var report = BuildMinimalReport() with
        {
            RoslynSummary = new AuditRoslynSummary(
                true, "/workspace/App.sln", DotNetWorkspaceKind.Sln,
                3, 42, 120, 40, 10, 70, [], null)
        };

        var markdown = new LegacyAuditMarkdownFormatter().Format(report);

        Assert.Contains("App.sln", markdown);
        Assert.Contains("Projects: 3", markdown);
        Assert.Contains("Documents: 42", markdown);
        Assert.Contains("Symbols: 120", markdown);
    }

    [Fact]
    public void Format_IncludesRoslynUnavailable_WhenNotLoaded()
    {
        var report = BuildMinimalReport() with
        {
            RoslynSummary = new AuditRoslynSummary(
                false, null, null, 0, 0, 0, 0, 0, 0, [], "No .sln found.")
        };

        var markdown = new LegacyAuditMarkdownFormatter().Format(report);

        Assert.Contains("Roslyn workspace could not be loaded", markdown);
        Assert.Contains("No .sln found.", markdown);
    }

    [Fact]
    public void Format_IncludesSyntaxOnlyCounts_WhenWorkspaceNotLoadedButFallbackFoundSymbols()
    {
        var report = BuildMinimalReport() with
        {
            RoslynSummary = new AuditRoslynSummary(
                false, "/workspace/App.sln", DotNetWorkspaceKind.Sln,
                0, 25, 100, 30, 5, 60, ["Syntax-only fallback used."], "MSBuild failed.")
        };

        var markdown = new LegacyAuditMarkdownFormatter().Format(report);

        Assert.Contains("Syntax-only C# fallback", markdown);
        Assert.Contains("Documents scanned: 25", markdown);
        Assert.Contains("Symbols: 100", markdown);
        Assert.Contains("Classes: 30, Interfaces: 5, Methods: 60", markdown);
    }

    [Fact]
    public void Format_IncludesDiSummary()
    {
        var report = BuildMinimalReport() with
        {
            DependencyInjectionSummary = new AuditDependencyInjectionSummary(
                5, 8, 1,
                new Dictionary<string, int> { ["Scoped"] = 3, ["Singleton"] = 2 },
                [new DependencyInjectionFinding("Warning", "multiple-registrations", "IFoo is registered 2 times.", null, null, null)])
        };

        var markdown = new LegacyAuditMarkdownFormatter().Format(report);

        Assert.Contains("Registrations: 5", markdown);
        Assert.Contains("multiple-registrations", markdown);
        Assert.Contains("Scoped: 3", markdown);
    }

    [Fact]
    public void Format_IncludesGpuSearchSummary_WhenAvailable()
    {
        var report = BuildMinimalReport() with
        {
            GpuSearchSummary = new AuditGpuSearchSummary(
                true, 3, 5,
                [new AuditGpuSearchResult("System.Web", "src/Startup.cs", 10, "using System.Web;")],
                null)
        };

        var markdown = new LegacyAuditMarkdownFormatter().Format(report);

        Assert.Contains("Queries run: 3", markdown);
        Assert.Contains("System.Web", markdown);
        Assert.Contains("heuristic", markdown);
    }

    [Fact]
    public void Format_IncludesGpuSearchUnavailable_WhenNotAvailable()
    {
        var report = BuildMinimalReport() with
        {
            GpuSearchSummary = new AuditGpuSearchSummary(false, 0, 0, [], "Connection refused")
        };

        var markdown = new LegacyAuditMarkdownFormatter().Format(report);

        Assert.Contains("unavailable or returned errors", markdown);
        Assert.Contains("Connection refused", markdown);
    }

    [Fact]
    public void Format_ShowsSignalScanMode_WhenUsedSignalScan()
    {
        var report = BuildMinimalReport() with
        {
            GpuSearchSummary = new AuditGpuSearchSummary(
                true, 5, 12,
                [new AuditGpuSearchResult("System.Web", "src/Startup.cs", 3, null)],
                null,
                UsedSignalScan: true,
                SignalCategories: ["Framework", "Quality"],
                ScanLimitations: ["Results are heuristic only."],
                ScanWarnings: null)
        };

        var markdown = new LegacyAuditMarkdownFormatter().Format(report);

        Assert.Contains("Signal scan", markdown);
        Assert.Contains("/scan/signals", markdown);
        Assert.Contains("Signals scanned: 5", markdown);
        Assert.Contains("Framework", markdown);
        Assert.Contains("Results are heuristic only.", markdown);
    }

    [Fact]
    public void Format_ShowsFallbackMode_WhenNotUsedSignalScan()
    {
        var report = BuildMinimalReport() with
        {
            GpuSearchSummary = new AuditGpuSearchSummary(
                true, 17, 0,
                [],
                null,
                UsedSignalScan: false)
        };

        var markdown = new LegacyAuditMarkdownFormatter().Format(report);

        Assert.Contains("Individual queries (fallback)", markdown);
        Assert.Contains("Queries run: 17", markdown);
    }


    [Fact]
    public void Format_IncludesGpuSearchIndexStatus_WhenPresent()
    {
        var report = BuildMinimalReport() with
        {
            GpuSearchSummary = new AuditGpuSearchSummary(
                true,
                2,
                4,
                [],
                null,
                UsedSignalScan: true,
                IndexStatus: "indexed selected repository",
                IndexedRoot: "D:/Repos/App",
                IndexMessage: "indexed")
        };

        var markdown = new LegacyAuditMarkdownFormatter().Format(report);

        Assert.Contains("Index status: indexed selected repository", markdown);
        Assert.Contains("Indexed root: `D:/Repos/App`", markdown);
        Assert.Contains("Index message: indexed", markdown);
    }
    [Fact]
    public void Format_OmitsRoslynSection_WhenNotRequested()
    {
        var report = BuildMinimalReport() with { RoslynSummary = null };

        var markdown = new LegacyAuditMarkdownFormatter().Format(report);

        Assert.Contains("## Roslyn Summary", markdown);
        Assert.Contains("not requested", markdown);
    }

    [Fact]
    public void Format_IncludesLlmSummary_WhenPresent()
    {
        var report = BuildMinimalReport() with { LlmSummary = "This is a legacy application." };

        var markdown = new LegacyAuditMarkdownFormatter().Format(report);

        Assert.Contains("## LLM Summary", markdown);
        Assert.Contains("This is a legacy application.", markdown);
    }

    [Fact]
    public void Format_OmitsLlmSection_WhenAbsent()
    {
        var report = BuildMinimalReport() with { LlmSummary = null };

        var markdown = new LegacyAuditMarkdownFormatter().Format(report);

        Assert.DoesNotContain("## LLM Summary", markdown);
    }

    [Fact]
    public void Format_IncludesLimitationsSection()
    {
        var report = BuildMinimalReport();

        var markdown = new LegacyAuditMarkdownFormatter().Format(report);

        Assert.Contains("static analysis only", markdown);
        Assert.Contains("compiler-aware", markdown);
        Assert.Contains("heuristic", markdown);
        Assert.Contains("advisory", markdown);
    }

    [Fact]
    public void Format_IncludesRecommendedNextSteps()
    {
        var report = BuildMinimalReport() with
        {
            RecommendedNextSteps = ["Migrate to .NET 8.", "Add unit tests."]
        };

        var markdown = new LegacyAuditMarkdownFormatter().Format(report);

        Assert.Contains("Migrate to .NET 8.", markdown);
        Assert.Contains("Add unit tests.", markdown);
    }

    private static LegacyAuditReport BuildMinimalReport()
    {
        return new LegacyAuditReport(
            "test-report",
            "D:/TestRepo",
            DateTimeOffset.UnixEpoch,
            "Audit complete.",
            new AuditWorkspaceSummary(null, null, 0, 0, 0, 0, []),
            [],
            [],
            [],
            null,
            null,
            null,
            [],
            null,
            string.Empty);
    }
}

