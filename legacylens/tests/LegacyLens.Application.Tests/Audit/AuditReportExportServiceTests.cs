using LegacyLens.Application.Audit;
using LegacyLens.Application.DotNetAnalysis;

namespace LegacyLens.Application.Tests.Audit;

public sealed class AuditReportExportServiceTests
{
    private readonly AuditReportExportService _service = new(new LegacyAuditMarkdownFormatter());

    [Fact]
    public void ExportMarkdown_ReturnsExistingMarkdown()
    {
        var report = MakeReport(markdown: "# Existing");

        var markdown = _service.ExportMarkdown(report);

        Assert.Equal("# Existing", markdown);
    }

    [Fact]
    public void ExportMarkdown_GeneratesFallbackWhenMarkdownMissing()
    {
        var report = MakeReport(markdown: string.Empty);

        var markdown = _service.ExportMarkdown(report);

        Assert.Contains("# Legacy .NET Audit Report", markdown);
        Assert.Contains("Summary text", markdown);
    }

    [Fact]
    public void ExportHtml_EscapesContent()
    {
        var report = MakeReport(
            repoPath: @"C:\Repos\<script>alert(1)</script>",
            summary: "Summary <b>unsafe</b>",
            markdown: "# Audit\n\n<script>alert(1)</script>");

        var html = _service.ExportHtml(report);

        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html);
        Assert.Contains("Summary &lt;b&gt;unsafe&lt;/b&gt;", html);
        Assert.DoesNotContain("<script>alert(1)</script>", html);
    }

    [Fact]
    public void ExportHtml_IncludesExpectedSections()
    {
        var html = _service.ExportHtml(MakeReport());

        Assert.Contains("<h1>Legacy .NET Audit Report</h1>", html);
        Assert.Contains("<h2>Summary</h2>", html);
        Assert.Contains("<h2>Risk Findings</h2>", html);
        Assert.Contains("<h2>Recommended Next Steps</h2>", html);
        Assert.Contains("<h2>Markdown Source</h2>", html);
    }

    [Fact]
    public void ExportHtml_IsSelfContainedWithoutExternalCdnLinks()
    {
        var html = _service.ExportHtml(MakeReport());

        Assert.DoesNotContain("cdn.", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<link", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildFileName_SanitizesRepositoryNameAndUsesTimestamp()
    {
        var report = MakeReport(repoPath: @"C:\Repos\Legacy App<>", generatedAtUtc: new DateTimeOffset(2026, 5, 16, 12, 34, 56, TimeSpan.Zero));

        var fileName = _service.BuildFileName(report, ".html");

        Assert.Equal("legacy-audit-Legacy-App-20260516-123456.html", fileName);
    }

    private static LegacyAuditReport MakeReport(
        string repoPath = @"C:\Repos\LegacyApp",
        string summary = "Summary text",
        string markdown = "# Audit",
        DateTimeOffset? generatedAtUtc = null)
    {
        return new LegacyAuditReport(
            "audit-1",
            repoPath,
            generatedAtUtc ?? DateTimeOffset.UnixEpoch,
            summary,
            new AuditWorkspaceSummary(Path.Combine(repoPath, "App.sln"), DotNetWorkspaceKind.Sln, 1, 0, 1, 0, []),
            [new TechnologySignal("ASP.NET MVC", "Framework", "App_Start found", null, "high")],
            [new ArchitectureSignal("Legacy app", "Legacy signals detected.", "Multiple signals", "medium")],
            [new AuditFinding("Warning", "web-config-present", "web.config present", "Legacy web.config.", "web.config")],
            null,
            null,
            null,
            ["Review modernization plan."],
            null,
            markdown);
    }
}
