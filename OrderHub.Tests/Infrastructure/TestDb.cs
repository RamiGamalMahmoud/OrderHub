using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderHub.Infrastructure;
using System;

namespace OrderHub.Tests.Infrastructure;

internal static class TestDb
{
    public static TestDbFactory CreateFactory() => new();
}

// One connection owns one isolated in-memory database for the lifetime of a test.
internal sealed class TestDbFactory : AppDbContextFactory, IDisposable
{
    private readonly SqliteConnection _connection = new("Filename=:memory:");
    private bool _disposed;

    public TestDbFactory() : base("")
    {
        try
        {
            _connection.Open();
            using var context = CreateDbContext();
            context.Database.EnsureCreated();
        }
        catch
        {
            _connection.Dispose();
            throw;
        }
    }

    public override AppDbContext CreateDbContext()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .UseSnakeCaseNamingConvention()
            .Options);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _connection.Dispose();
        _disposed = true;
    }
}
