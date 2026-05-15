using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using RefactorGuard.Application.Reports;
using RefactorGuard.Application.Review;

namespace RefactorGuard.Infrastructure.Persistence;

public sealed class SqliteReportRepository(IOptions<PersistenceOptions> options) : IReportRepository
{
    private readonly string _databasePath = options.Value.DatabasePath;

    public async Task SaveAsync(DiffReviewReport report, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO reports (
                report_id,
                repo_path,
                generated_at_utc,
                changed_file_count,
                llm_provider,
                json
            )
            VALUES ($report_id, $repo_path, $generated_at_utc, $changed_file_count, $llm_provider, $json)
            ON CONFLICT(report_id) DO UPDATE SET
                repo_path = excluded.repo_path,
                generated_at_utc = excluded.generated_at_utc,
                changed_file_count = excluded.changed_file_count,
                llm_provider = excluded.llm_provider,
                json = excluded.json;
            """;
        command.Parameters.AddWithValue("$report_id", report.ReportId);
        command.Parameters.AddWithValue("$repo_path", report.RepoPath);
        command.Parameters.AddWithValue("$generated_at_utc", report.GeneratedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$changed_file_count", report.ChangedFileCount);
        command.Parameters.AddWithValue("$llm_provider", report.LlmProvider);
        command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(report, JsonOptions));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<DiffReviewReport?> GetByIdAsync(string reportId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT json FROM reports WHERE report_id = $report_id;";
        command.Parameters.AddWithValue("$report_id", reportId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is string json
            ? JsonSerializer.Deserialize<DiffReviewReport>(json, JsonOptions)
            : null;
    }

    public async Task<IReadOnlyList<ReportSummary>> ListAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT report_id, repo_path, generated_at_utc, changed_file_count, llm_provider
            FROM reports
            ORDER BY generated_at_utc DESC;
            """;

        var reports = new List<ReportSummary>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            reports.Add(new ReportSummary(
                reader.GetString(0),
                reader.GetString(1),
                DateTimeOffset.Parse(reader.GetString(2)),
                reader.GetInt32(3),
                reader.GetString(4)));
        }

        return reports;
    }

    public async Task<bool> DeleteAsync(string reportId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM reports WHERE report_id = $report_id;";
        command.Parameters.AddWithValue("$report_id", reportId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath
        };
        var connection = new SqliteConnection(builder.ToString());
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static async Task EnsureSchemaAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS reports (
                report_id TEXT PRIMARY KEY,
                repo_path TEXT NOT NULL,
                generated_at_utc TEXT NOT NULL,
                changed_file_count INTEGER NOT NULL,
                llm_provider TEXT NOT NULL,
                json TEXT NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
