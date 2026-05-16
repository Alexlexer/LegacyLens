using Microsoft.Extensions.Options;
using LegacyLens.Application.Audit;
using LegacyLens.Application.DotNetAnalysis;
using LegacyLens.Application.Reports;
using LegacyLens.Application.Review;
using LegacyLens.Domain.Git;
using LegacyLens.Infrastructure.Persistence;

namespace LegacyLens.Infrastructure.Tests.Persistence;

public sealed class SqliteReportRepositoryTests : IDisposable
{
    private readonly string _directory = Directory.CreateDirectory(
        Path.Combine(Path.GetTempPath(), $"LegacyLens-db-{Guid.NewGuid():N}")).FullName;

    [Fact]
    public async Task SaveGetListDelete_RoundTripsReport()
    {
        var repository = CreateRepository();
        var report = new DiffReviewReport(
            "report-1",
            "repo",
            DateTimeOffset.UnixEpoch,
            1,
            [new GitDiffFile("src/App.cs", "M", 2, 1)],
            [new ReviewFinding("rule", "Info", "src/App.cs", "Title", "Description")],
            "# Report",
            null,
            "Deterministic");

        await repository.SaveAsync(report, CancellationToken.None);

        var fetched = await repository.GetByIdAsync("report-1", CancellationToken.None);
        var list = await repository.ListAsync(CancellationToken.None);
        var deleted = await repository.DeleteAsync("report-1", CancellationToken.None);
        var missing = await repository.GetByIdAsync("report-1", CancellationToken.None);

        Assert.NotNull(fetched);
        Assert.Equal("# Report", fetched.Markdown);
        Assert.Single(list);
        Assert.True(deleted);
        Assert.Null(missing);
    }

    [Fact]
    public async Task SaveAuditGetAuditListDelete_RoundTripsAuditReport()
    {
        var repository = CreateRepository();
        var report = MakeAuditReport("audit-1", @"C:\projects\MyApp");

        await repository.SaveAuditAsync(report, CancellationToken.None);

        var fetched = await repository.GetAuditByIdAsync("audit-1", CancellationToken.None);
        var list = await repository.ListAsync(CancellationToken.None);
        var deleted = await repository.DeleteAsync("audit-1", CancellationToken.None);
        var missing = await repository.GetAuditByIdAsync("audit-1", CancellationToken.None);

        Assert.NotNull(fetched);
        Assert.Equal("# Audit", fetched.Markdown);
        Assert.Single(list);
        Assert.Equal(ReportType.LegacyAudit, list[0].ReportType);
        Assert.Equal("MyApp", list[0].Title);
        Assert.True(deleted);
        Assert.Null(missing);
    }

    [Fact]
    public async Task SavedAuditReport_CanBeExportedAfterRetrieval()
    {
        var repository = CreateRepository();
        var exportService = new AuditReportExportService(new LegacyAuditMarkdownFormatter());
        await repository.SaveAuditAsync(MakeAuditReport("audit-export", @"C:\projects\MyApp"), CancellationToken.None);

        var fetched = await repository.GetAuditByIdAsync("audit-export", CancellationToken.None);

        Assert.NotNull(fetched);
        Assert.Equal("# Audit", exportService.ExportMarkdown(fetched));
        Assert.Contains("<h1>Legacy .NET Audit Report</h1>", exportService.ExportHtml(fetched));
    }

    [Fact]
    public async Task ListAsync_ReturnsBothReportTypes()
    {
        var repository = CreateRepository();
        var diff = new DiffReviewReport(
            "diff-1", "repo", DateTimeOffset.UnixEpoch, 1,
            [new GitDiffFile("src/App.cs", "M", 2, 1)],
            [new ReviewFinding("rule", "Info", "src/App.cs", "Title", "Description")],
            "# Report", null, "Deterministic");
        var audit = MakeAuditReport("audit-1", @"C:\projects\MyApp");

        await repository.SaveAsync(diff, CancellationToken.None);
        await repository.SaveAuditAsync(audit, CancellationToken.None);

        var list = await repository.ListAsync(CancellationToken.None);

        Assert.Equal(2, list.Count);
        Assert.Contains(list, r => r.ReportType == ReportType.DiffReview);
        Assert.Contains(list, r => r.ReportType == ReportType.LegacyAudit);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullForAuditReportId()
    {
        var repository = CreateRepository();
        await repository.SaveAuditAsync(MakeAuditReport("audit-1", @"C:\projects\MyApp"), CancellationToken.None);

        var result = await repository.GetByIdAsync("audit-1", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAuditByIdAsync_ReturnsNullForDiffReportId()
    {
        var repository = CreateRepository();
        var diff = new DiffReviewReport(
            "diff-1", "repo", DateTimeOffset.UnixEpoch, 1,
            [new GitDiffFile("src/App.cs", "M", 2, 1)],
            [new ReviewFinding("rule", "Info", "src/App.cs", "Title", "Description")],
            "# Report", null, "Deterministic");
        await repository.SaveAsync(diff, CancellationToken.None);

        var result = await repository.GetAuditByIdAsync("diff-1", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task EnsureSchema_IsIdempotent()
    {
        // Two repositories pointing at the same DB file — each call EnsureSchemaAsync on open.
        var db = Path.Combine(_directory, "idempotent.db");
        var opts = Options.Create(new PersistenceOptions { DatabasePath = db });

        var repo1 = new SqliteReportRepository(opts);
        await repo1.SaveAsync(new DiffReviewReport(
            "r1", "repo", DateTimeOffset.UnixEpoch, 0, [], [], "md", null, "Deterministic"),
            CancellationToken.None);

        var repo2 = new SqliteReportRepository(opts);
        var list = await repo2.ListAsync(CancellationToken.None);

        Assert.Single(list);
        Assert.Equal("r1", list[0].ReportId);
    }

    [Fact]
    public async Task SaveAuditAsync_WithLlmSummary_SetsProviderToLlm()
    {
        var repository = CreateRepository();
        var report = MakeAuditReport("audit-llm", @"C:\projects\Svc", llmSummary: "Great code.");

        await repository.SaveAuditAsync(report, CancellationToken.None);

        var list = await repository.ListAsync(CancellationToken.None);
        Assert.Single(list);
        Assert.Equal("LLM", list[0].LlmProvider);
    }

    [Fact]
    public async Task SaveAuditAsync_WithoutLlmSummary_SetsProviderToDeterministic()
    {
        var repository = CreateRepository();
        var report = MakeAuditReport("audit-det", @"C:\projects\Svc");

        await repository.SaveAuditAsync(report, CancellationToken.None);

        var list = await repository.ListAsync(CancellationToken.None);
        Assert.Single(list);
        Assert.Equal("Deterministic", list[0].LlmProvider);
    }

    public void Dispose()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    private SqliteReportRepository CreateRepository()
    {
        return new SqliteReportRepository(Options.Create(new PersistenceOptions
        {
            DatabasePath = Path.Combine(_directory, "reports.db")
        }));
    }

    private static LegacyAuditReport MakeAuditReport(string id, string repoPath, string? llmSummary = null)
    {
        var workspace = new AuditWorkspaceSummary(repoPath, DotNetWorkspaceKind.Sln, 1, 0, 1, 0, []);
        return new LegacyAuditReport(
            id,
            repoPath,
            DateTimeOffset.UnixEpoch,
            "Summary text",
            workspace,
            [],
            [],
            [],
            null,
            null,
            null,
            [],
            llmSummary,
            "# Audit");
    }
}
