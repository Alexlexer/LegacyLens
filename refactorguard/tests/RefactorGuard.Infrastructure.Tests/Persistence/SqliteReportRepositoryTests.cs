using Microsoft.Extensions.Options;
using RefactorGuard.Application.Review;
using RefactorGuard.Domain.Git;
using RefactorGuard.Infrastructure.Persistence;

namespace RefactorGuard.Infrastructure.Tests.Persistence;

public sealed class SqliteReportRepositoryTests : IDisposable
{
    private readonly string _directory = Directory.CreateDirectory(
        Path.Combine(Path.GetTempPath(), $"refactorguard-db-{Guid.NewGuid():N}")).FullName;

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
}
