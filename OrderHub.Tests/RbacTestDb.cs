using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Migrations;
using OrderHub.Infrastructure.Services;
using System;
using System.Threading.Tasks;

namespace OrderHub.Tests;

internal sealed class RbacTestDb : IDisposable
{
    private readonly SqliteConnection _connection;

    public RbacTestDb()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        Options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .UseSnakeCaseNamingConvention()
            .Options;

        Context = new AppDbContext(Options);
        Context.Database.EnsureCreated();

        UnitOfWork = new UnitOfWork(
            Context,
            new NumberSequenceService(
                Context,
                new AppDbContextFactory(Options)));
    }

    public DbContextOptions<AppDbContext> Options { get; }

    public AppDbContext Context { get; }

    public UnitOfWork UnitOfWork { get; }

    public Task SeedAsync() =>
        Context.Database.ExecuteSqlRawAsync(SealedRoleSeedSql.V1);

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
