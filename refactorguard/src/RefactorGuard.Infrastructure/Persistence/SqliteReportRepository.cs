using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using RefactorGuard.Application.Audit;
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

    public async Task SaveAuditAsync(LegacyAuditReport report, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);

        var title = Path.GetFileName(report.RepoPath.TrimEnd(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var llmProvider = report.LlmSummary is not null ? "LLM" : "Deterministic";

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO reports (
                report_id,
                repo_path,
                generated_at_utc,
                changed_file_count,
                llm_provider,
                json,
                report_type,
                title
            )
            VALUES ($report_id, $repo_path, $generated_at_utc, 0, $llm_provider, $json, $report_type, $title)
            ON CONFLICT(report_id) DO UPDATE SET
                repo_path = excluded.repo_path,
                generated_at_utc = excluded.generated_at_utc,
                changed_file_count = excluded.changed_file_count,
                llm_provider = excluded.llm_provider,
                json = excluded.json,
                report_type = excluded.report_type,
                title = excluded.title;
            """;
        command.Parameters.AddWithValue("$report_id", report.ReportId);
        command.Parameters.AddWithValue("$repo_path", report.RepoPath);
        command.Parameters.AddWithValue("$generated_at_utc", report.GeneratedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$llm_provider", llmProvider);
        command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(report, JsonOptions));
        command.Parameters.AddWithValue("$report_type", ReportType.LegacyAudit);
        command.Parameters.AddWithValue("$title", string.IsNullOrEmpty(title) ? report.RepoPath : title);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<DiffReviewReport?> GetByIdAsync(string reportId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT json, report_type FROM reports WHERE report_id = $report_id;";
        command.Parameters.AddWithValue("$report_id", reportId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        var reportType = reader.IsDBNull(1) ? ReportType.DiffReview : reader.GetString(1);
        if (!string.Equals(reportType, ReportType.DiffReview, StringComparison.OrdinalIgnoreCase))
            return null;

        return JsonSerializer.Deserialize<DiffReviewReport>(reader.GetString(0), JsonOptions);
    }

    public async Task<LegacyAuditReport?> GetAuditByIdAsync(string reportId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT json, report_type FROM reports WHERE report_id = $report_id;";
        command.Parameters.AddWithValue("$report_id", reportId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        var reportType = reader.IsDBNull(1) ? ReportType.DiffReview : reader.GetString(1);
        if (!string.Equals(reportType, ReportType.LegacyAudit, StringComparison.OrdinalIgnoreCase))
            return null;

        return JsonSerializer.Deserialize<LegacyAuditReport>(reader.GetString(0), JsonOptions);
    }

    public async Task<IReadOnlyList<ReportSummary>> ListAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT report_id, repo_path, generated_at_utc, changed_file_count, llm_provider, report_type, title
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
                reader.GetString(4),
                reader.IsDBNull(5) ? ReportType.DiffReview : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6)));
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
            Directory.CreateDirectory(directory);

        var builder = new SqliteConnectionStringBuilder { DataSource = _databasePath };
        var connection = new SqliteConnection(builder.ToString());
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static async Task EnsureSchemaAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var createCmd = connection.CreateCommand();
        createCmd.CommandText = """
            CREATE TABLE IF NOT EXISTS reports (
                report_id TEXT PRIMARY KEY,
                repo_path TEXT NOT NULL,
                generated_at_utc TEXT NOT NULL,
                changed_file_count INTEGER NOT NULL,
                llm_provider TEXT NOT NULL,
                json TEXT NOT NULL
            );
            """;
        await createCmd.ExecuteNonQueryAsync(cancellationToken);

        await AddColumnIfMissingAsync(connection, "reports", "report_type",
            "TEXT NOT NULL DEFAULT 'DiffReview'", cancellationToken);
        await AddColumnIfMissingAsync(connection, "reports", "title",
            "TEXT NOT NULL DEFAULT ''", cancellationToken);
    }

    private static async Task AddColumnIfMissingAsync(
        SqliteConnection connection,
        string table,
        string column,
        string definition,
        CancellationToken cancellationToken)
    {
        var columns = new List<string>();
        await using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = $"PRAGMA table_info({table});";
            await using var reader = await pragma.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                columns.Add(reader.GetString(1));
        }

        if (columns.Any(c => string.Equals(c, column, StringComparison.OrdinalIgnoreCase)))
            return;

        await using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition};";
        await alter.ExecuteNonQueryAsync(cancellationToken);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
