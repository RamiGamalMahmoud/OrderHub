using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderHub.Infrastructure;

namespace OrderHub.Tests;

/// <summary>
/// Owns the single PostgreSQL database used by database-backed tests.
///
/// The connection string must come from <c>ORDERHUB_TEST_POSTGRES</c> and must
/// target a database named <c>orderhub_test</c>. Migrations are applied once,
/// an advisory lock prevents two test runs from sharing the database, and every
/// test starts from an empty schema (except the EF migrations history table).
/// </summary>
internal static class PostgresTestDatabase
{
    public const string ConnectionStringVariable = "ORDERHUB_TEST_POSTGRES";
    public const string RequiredDatabaseName = "orderhub_test";

    private const string MigrationsHistoryTable = "__EFMigrationsHistory";
    private const long AdvisoryLockKey = 4_242_424_242_001;

    private static readonly Lazy<State> LazyState =
        new(Initialize, LazyThreadSafetyMode.ExecutionAndPublication);

    static PostgresTestDatabase()
    {
        // Releasing the lock connection lets a subsequent run acquire the lock.
        // Only dispose when a run actually initialized the database.
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            if (LazyState.IsValueCreated) LazyState.Value.Dispose();
        };
    }

    public static DbContextOptions<AppDbContext> Options => LazyState.Value.Options;

    public static string ConnectionString => LazyState.Value.ConnectionString;

    public static AppDbContext CreateContext() => new(Options);

    /// <summary>Truncates every public table except the migrations history.</summary>
    public static void Reset() => LazyState.Value.Reset();

    public static async Task ResetAsync(CancellationToken cancellationToken = default) =>
        await LazyState.Value.ResetAsync(cancellationToken);

    private static State Initialize()
    {
        var rawConnectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (string.IsNullOrWhiteSpace(rawConnectionString))
        {
            throw new InvalidOperationException(
                $"Database-backed tests require the {ConnectionStringVariable} environment variable. " +
                "Start the test database with " +
                "`docker compose -f OrderHub.Tests/Infrastructure/docker-compose.test.yml up -d` " +
                $"and set {ConnectionStringVariable} to a PostgreSQL connection string whose " +
                $"database is '{RequiredDatabaseName}'.");
        }

        var builder = new NpgsqlConnectionStringBuilder(rawConnectionString);
        if (!string.Equals(builder.Database, RequiredDatabaseName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{ConnectionStringVariable} must target the dedicated test database " +
                $"'{RequiredDatabaseName}', but it targets '{builder.Database ?? "(none)"}'. " +
                "Refusing to run tests against a non-test database.");
        }

        var connectionString = builder.ConnectionString;
        var lockConnection = new NpgsqlConnection(connectionString);
        lockConnection.Open();

        try
        {
            using var lockCommand = lockConnection.CreateCommand();
            lockCommand.CommandText = "SELECT pg_try_advisory_lock(@key)";
            lockCommand.Parameters.AddWithValue("key", AdvisoryLockKey);
            var acquired = (bool)(lockCommand.ExecuteScalar() ?? false);
            if (!acquired)
            {
                throw new InvalidOperationException(
                    $"Another test run already holds the PostgreSQL advisory lock for '{RequiredDatabaseName}'. " +
                    "Database-backed tests share one schema and cannot run concurrently across processes.");
            }
        }
        catch
        {
            lockConnection.Dispose();
            throw;
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        try
        {
            using var context = new AppDbContext(options);
            context.Database.Migrate();
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.InvalidCatalogName)
        {
            lockConnection.Dispose();
            throw new InvalidOperationException(
                $"The PostgreSQL database '{RequiredDatabaseName}' does not exist. " +
                "Start the test database with " +
                "`docker compose -f OrderHub.Tests/Infrastructure/docker-compose.test.yml up -d` " +
                "and wait for its health check to pass.",
                exception);
        }
        catch
        {
            lockConnection.Dispose();
            throw;
        }

        var state = new State(connectionString, options, lockConnection);
        state.Reset();
        return state;
    }

    private sealed class State(
        string connectionString,
        DbContextOptions<AppDbContext> options,
        NpgsqlConnection lockConnection) : IDisposable
    {
        public string ConnectionString { get; } = connectionString;

        public DbContextOptions<AppDbContext> Options { get; } = options;

        public void Reset() => ResetAsync(CancellationToken.None).GetAwaiter().GetResult();

        public async Task ResetAsync(CancellationToken cancellationToken = default)
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var tables = new List<string>();
            await using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    """
                    SELECT table_name
                    FROM information_schema.tables
                    WHERE table_schema = 'public'
                      AND table_type = 'BASE TABLE'
                      AND table_name <> @history
                    ORDER BY table_name
                    """;
                command.Parameters.AddWithValue("history", MigrationsHistoryTable);

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                    tables.Add(reader.GetString(0));
            }

            if (tables.Count == 0)
                return;

            var quotedTables = string.Join(
                ", ",
                tables.Select(table => "\"public\".\"" + table.Replace("\"", "\"\"") + "\""));

            await using var truncate = connection.CreateCommand();
            truncate.CommandText = $"TRUNCATE TABLE {quotedTables} RESTART IDENTITY CASCADE";
            await truncate.ExecuteNonQueryAsync(cancellationToken);
        }

        public void Dispose() => lockConnection.Dispose();
    }
}
