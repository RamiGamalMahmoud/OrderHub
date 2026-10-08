using System;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderHub.Infrastructure;

namespace OrderHub.Tests.Infrastructure;

public sealed class LifecyclePostgresFactAttribute : FactAttribute
{
    public LifecyclePostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ORDERHUB_LIFECYCLE_TEST_POSTGRES")))
            Skip = "Set ORDERHUB_LIFECYCLE_TEST_POSTGRES to a disposable PostgreSQL test server.";
    }
}

internal sealed class OrderLifecyclePostgresDatabase : AppDbContextFactory, IDisposable
{
    private readonly string _admin = Environment.GetEnvironmentVariable("ORDERHUB_LIFECYCLE_TEST_POSTGRES");
    private readonly string _database = "orderhub_lifecycle_test_" + Guid.NewGuid().ToString("N");
    private readonly string _connection;

    internal string ConnectionString => _connection;

    public OrderLifecyclePostgresDatabase(bool createSchema = true) : base("")
    {
        _connection = new NpgsqlConnectionStringBuilder(_admin) { Database = _database, Pooling = false }.ConnectionString;
        using var admin = new NpgsqlConnection(_admin);
        admin.Open();
        using var create = new NpgsqlCommand($"CREATE DATABASE \"{_database}\"", admin);
        create.ExecuteNonQuery();
        try
        {
            if (createSchema)
            {
                using var db = CreateDbContext();
                db.Database.EnsureCreated();
            }
        }
        catch { Dispose(); throw; }
    }

    public override AppDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(_connection).UseSnakeCaseNamingConvention().Options);

    public void Dispose()
    {
        if (!_database.StartsWith("orderhub_lifecycle_test_", StringComparison.Ordinal))
            throw new InvalidOperationException("Unexpected test database.");
        using var admin = new NpgsqlConnection(_admin);
        admin.Open();
        using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_database}\" WITH (FORCE)", admin);
        drop.ExecuteNonQuery();
    }
}
