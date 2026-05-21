using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace BaqueanoAutoTest.Infrastructure;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(IConfiguration configuration)
    {
        var raw = configuration.GetConnectionString("SQLite") ?? "Data Source=Data\\baqueano_tests.db";

        // Resolve a relative DataSource to the executable's directory so the DB
        // is always written next to the binary regardless of the working directory
        var csb = new SqliteConnectionStringBuilder(raw);
        if (!Path.IsPathRooted(csb.DataSource))
            csb.DataSource = Path.Combine(AppContext.BaseDirectory, csb.DataSource);

        _connectionString = csb.ToString();
    }

    public async Task InitializeAsync()
    {
        var builder = new SqliteConnectionStringBuilder(_connectionString);
        var dir = Path.GetDirectoryName(builder.DataSource);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS TestResults (
                Id             INTEGER PRIMARY KEY AUTOINCREMENT,
                TestName       TEXT    NOT NULL,
                Category       TEXT    NOT NULL,
                Passed         INTEGER NOT NULL,
                Message        TEXT,
                ScreenshotPath TEXT,
                ExecutedAt     TEXT    NOT NULL
            );
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SaveResultAsync(TestResult result)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO TestResults (TestName, Category, Passed, Message, ScreenshotPath, ExecutedAt)
            VALUES ($name, $category, $passed, $message, $screenshot, $executedAt);
            """;
        cmd.Parameters.AddWithValue("$name", result.TestName);
        cmd.Parameters.AddWithValue("$category", result.Category);
        cmd.Parameters.AddWithValue("$passed", result.Passed ? 1 : 0);
        cmd.Parameters.AddWithValue("$message", result.Message ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$screenshot", result.ScreenshotPath ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$executedAt", result.ExecutedAt.ToString("O"));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ClearAllAsync()
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM TestResults;";
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<TestResult>> GetAllResultsAsync()
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM TestResults ORDER BY ExecutedAt DESC;";

        var results = new List<TestResult>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new TestResult
            {
                Id = reader.GetInt32(0),
                TestName = reader.GetString(1),
                Category = reader.GetString(2),
                Passed = reader.GetInt32(3) == 1,
                Message = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                ScreenshotPath = reader.IsDBNull(5) ? null : reader.GetString(5),
                ExecutedAt = DateTime.Parse(reader.GetString(6))
            });
        }
        return results;
    }
}
