using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using LegacyLens.Application.Audit;
using LegacyLens.Application.DotNetAnalysis;
using LegacyLens.Application.Reports;
using LegacyLens.Application.Review;

namespace LegacyLens.Api.Tests;

public sealed class AuditReportExportEndpointTests
{
    [Fact]
    public async Task MarkdownExport_ReturnsMarkdownAttachment()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/audit/reports/audit-1/export/markdown");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/markdown", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("utf-8", response.Content.Headers.ContentType?.CharSet);
        Assert.Contains("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
        Assert.EndsWith(".md", response.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
        Assert.Equal("# Saved Audit", body);
    }

    [Fact]
    public async Task HtmlExport_ReturnsHtmlAttachment()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/audit/reports/audit-1/export/html");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("utf-8", response.Content.Headers.ContentType?.CharSet);
        Assert.Contains("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
        Assert.EndsWith(".html", response.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
        Assert.Contains("<h1>Legacy .NET Audit Report</h1>", body);
    }

    [Fact]
    public async Task Export_Returns404ForMissingReport()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/audit/reports/missing/export/html");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Export_Returns404ForWrongReportType()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/audit/reports/diff-1/export/markdown");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IReportRepository>();
                    services.AddSingleton<IReportRepository, StubReportRepository>();
                });
            });
    }

    private sealed class StubReportRepository : IReportRepository
    {
        public Task SaveAsync(DiffReviewReport report, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<DiffReviewReport?> GetByIdAsync(string reportId, CancellationToken cancellationToken)
            => Task.FromResult<DiffReviewReport?>(reportId == "diff-1"
                ? new DiffReviewReport("diff-1", "repo", DateTimeOffset.UnixEpoch, 0, [], [], "# Diff", null, "Deterministic")
                : null);

        public Task<IReadOnlyList<ReportSummary>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ReportSummary>>([]);

        public Task<bool> DeleteAsync(string reportId, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task SaveAuditAsync(LegacyAuditReport report, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<LegacyAuditReport?> GetAuditByIdAsync(string reportId, CancellationToken cancellationToken)
            => Task.FromResult(reportId == "audit-1" ? MakeAuditReport() : null);

        private static LegacyAuditReport MakeAuditReport()
        {
            return new LegacyAuditReport(
                "audit-1",
                @"C:\projects\MyApp",
                DateTimeOffset.UnixEpoch,
                "Summary text",
                new AuditWorkspaceSummary(@"C:\projects\MyApp\App.sln", DotNetWorkspaceKind.Sln, 1, 0, 1, 0, []),
                [],
                [],
                [],
                null,
                null,
                null,
                [],
                null,
                "# Saved Audit");
        }
    }
}
