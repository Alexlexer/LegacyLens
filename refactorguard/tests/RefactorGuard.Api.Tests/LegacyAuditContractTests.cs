using RefactorGuard.Application.Audit;
using RefactorGuard.Application.DotNetAnalysis;

namespace RefactorGuard.Api.Tests;

public sealed class LegacyAuditContractTests
{
    [Fact]
    public void LegacyAuditReport_ExposesStableContract()
    {
        var report = new LegacyAuditReport(
            "audit-1",
            "D:/Projects/Repo",
            DateTimeOffset.UnixEpoch,
            "Audit complete.",
            new AuditWorkspaceSummary("/workspace/App.sln", DotNetWorkspaceKind.Sln, 3, 0, 1, 2, []),
            [new TechnologySignal("ASP.NET MVC", "Framework", "App_Start found", null, "high")],
            [new ArchitectureSignal("Legacy app", "Legacy signals detected.", "Multiple signals", "medium")],
            [new AuditFinding("Warning", "web-config-present", "web.config present", "Legacy web.config.", "web.config")],
            null,
            null,
            null,
            ["Upgrade to .NET 8."],
            null,
            "# Audit Report");

        Assert.Equal("audit-1", report.ReportId);
        Assert.Equal("D:/Projects/Repo", report.RepoPath);
        Assert.Single(report.TechnologySignals);
        Assert.Single(report.ArchitectureSignals);
        Assert.Single(report.RiskFindings);
        Assert.Single(report.RecommendedNextSteps);
        Assert.Equal("# Audit Report", report.Markdown);
        Assert.Null(report.LlmSummary);
        Assert.Null(report.RoslynSummary);
    }

    [Fact]
    public void LegacyAuditRequest_DefaultsToSafeValues()
    {
        var request = new LegacyAuditRequest("D:/Projects/Repo");

        Assert.False(request.UseLlm);
        Assert.True(request.IncludeRoslyn);
        Assert.True(request.IncludeGpuSearch);
        Assert.True(request.IncludeDotNetPresets);
        Assert.True(request.IncludeDependencyInjection);
    }

    [Fact]
    public void AuditRoslynSummary_ExposesStableContract()
    {
        var summary = new AuditRoslynSummary(
            true, "/workspace/App.sln", DotNetWorkspaceKind.Sln,
            3, 42, 120, 40, 10, 70,
            ["warning 1"], null);

        Assert.True(summary.WorkspaceLoaded);
        Assert.Equal(3, summary.ProjectCount);
        Assert.Equal(40, summary.ClassCount);
        Assert.Equal(10, summary.InterfaceCount);
        Assert.Equal(70, summary.MethodCount);
    }

    [Fact]
    public void AuditGpuSearchSummary_ExposesStableContract()
    {
        var summary = new AuditGpuSearchSummary(
            true, 5, 12,
            [new AuditGpuSearchResult("System.Web", "src/Startup.cs", 10, "using System.Web;")],
            null);

        Assert.True(summary.WasAvailable);
        Assert.Equal(5, summary.QueriesRun);
        Assert.Equal(12, summary.TotalResults);
        Assert.Single(summary.Results);
        Assert.Equal("System.Web", summary.Results[0].Query);
    }

    [Fact]
    public void AuditDependencyInjectionSummary_ExposesStableContract()
    {
        var summary = new AuditDependencyInjectionSummary(
            4, 6, 1,
            new Dictionary<string, int> { ["Scoped"] = 2, ["Singleton"] = 2 },
            [new DependencyInjectionFinding("Warning", "multiple-registrations", "IFoo registered 2 times.", null, null, null)]);

        Assert.Equal(4, summary.RegistrationCount);
        Assert.Equal(6, summary.ConstructorDependencyCount);
        Assert.Single(summary.Findings);
    }
}
