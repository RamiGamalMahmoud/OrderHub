using Microsoft.EntityFrameworkCore;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Migrations;
using OrderHub.Infrastructure.Services;
using System;
using System.Threading.Tasks;

namespace OrderHub.Tests;

internal sealed class RbacTestDb : IDisposable
{
    public RbacTestDb()
    {
        PostgresTestDatabase.Reset();

        Options = PostgresTestDatabase.Options;

        Context = new AppDbContext(Options);

        UnitOfWork = new UnitOfWork(
            Context,
            new NumberSequenceService(
                Context,
                new AppDbContextFactory(Options)));
    }

    public DbContextOptions<AppDbContext> Options { get; }

    public AppDbContext Context { get; }

    public UnitOfWork UnitOfWork { get; }

    public async Task SeedAsync()
    {
        await Context.Database.ExecuteSqlRawAsync(SealedRoleSeedSql.V1);
        await Context.Database.ExecuteSqlRawAsync(SealedRoleSeedSql.V2);
    }

    public void Dispose()
    {
        Context.Dispose();
    }
}
