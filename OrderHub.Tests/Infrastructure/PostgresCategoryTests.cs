using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using OrderHub.Application.Features.Setup.Categories;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Features.Setup.Categories;

namespace OrderHub.Tests.Infrastructure;

public class PostgresCategoryTests
{
    [CategoryPostgresFact]
    public async Task PostgreSQL_browser_uses_one_query_for_recursive_counts_and_breadcrumbs()
    {
        using var factory = new PostgresFactory();
        int deepId;
        using (var db = factory.CreateDbContext())
        {
            var root = new Category("Root");
            var child = new Category("Child", root);
            var deep = new Category("Deep", child);
            db.Categories.AddRange(root, child, deep, new Category("Other root"));
            db.Products.AddRange(new Product("Root product", "P1", root, new Money(10)),
                new Product("Deep product", "P2", deep, new Money(10)));
            await db.SaveChangesAsync();
            deepId = deep.Id;
        }
        factory.Counter.Reads = 0;
        var reader = new CategoryReader(factory);
        var roots = await reader.GetCategoriesAsync(null, default);
        Assert.Equal(1, factory.Counter.Reads);
        Assert.Equal(2, roots.Categories.Count);
        Assert.Equal(2, roots.Categories.Single(category => category.Name == "Root").TotalProductsCount);
        Assert.Equal(1, roots.Categories.Single(category => category.Name == "Root").DirectChildrenCount);
        factory.Counter.Reads = 0;
        var nested = await reader.GetCategoriesAsync(deepId, default);
        Assert.Equal(1, factory.Counter.Reads);
        Assert.Equal(new[] { "Root", "Child", "Deep" }, nested.Path.Select(segment => segment.Name));
    }

    [CategoryPostgresFact]
    public async Task PostgreSQL_ancestry_query_rejects_deep_cycle_and_allows_other_branch()
    {
        using var factory = new PostgresFactory();
        using var provider = Services(factory);
        int rootId, deepId, otherId;
        using (var db = factory.CreateDbContext())
        {
            var root = new Category("Root");
            var child = new Category("Child", root);
            var deep = new Category("Deep", child);
            var other = new Category("Other");
            db.Categories.AddRange(root, child, deep, other);
            await db.SaveChangesAsync();
            rootId = root.Id; deepId = deep.Id; otherId = other.Id;
        }
        Assert.False((await Execute(provider, new UpdateCategoryCommand(new(rootId, "Root", deepId)))).IsSuccess);
        Assert.True((await Execute(provider, new UpdateCategoryCommand(new(rootId, "Root", otherId)))).IsSuccess);
        Assert.Equal(otherId, (await Execute(provider, new GetCategoryForEditQuery(rootId))).ParentId);
    }

    [CategoryPostgresFact]
    public async Task PostgreSQL_concurrent_opposite_moves_cannot_create_a_cycle()
    {
        using var factory = new PostgresFactory();
        using var provider = Services(factory);
        int firstId, secondId;
        using (var db = factory.CreateDbContext())
        {
            var first = new Category("First");
            var second = new Category("Second");
            db.Categories.AddRange(first, second);
            await db.SaveChangesAsync();
            firstId = first.Id; secondId = second.Id;
        }
        var results = await Task.WhenAll(
            Execute(provider, new UpdateCategoryCommand(new(firstId, "First", secondId))),
            Execute(provider, new UpdateCategoryCommand(new(secondId, "Second", firstId))));
        Assert.Single(results, result => result.IsSuccess);
        Assert.Single(results, result => !result.IsSuccess);
        Assert.Single((await Execute(provider, new GetCategoriesQuery())).Categories);
    }

    private static ServiceProvider Services(PostgresFactory factory)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        OrderHub.Application.ServiceCollectionExtensions.AddApplication(services);
        OrderHub.Infrastructure.DependencyInjection.ServiceCollectionExtensions.AddInfrastructure(services, "unused");
        services.RemoveAll<AppDbContextFactory>();
        services.AddSingleton<AppDbContextFactory>(factory);
        services.AddScoped(_ => OrderHub.Tests.Application.AuthorizationTests.Identity(true, 1, true));
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static async Task<T> Execute<T>(ServiceProvider provider, IRequest<T> request)
    {
        await using var scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request);
    }

    // An explicit opt-in connection must point to a disposable test server whose user
    // can create databases. Every test creates and removes its own isolated database.
    public sealed class CategoryPostgresFactAttribute : FactAttribute
    {
        public CategoryPostgresFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ORDERHUB_CATEGORY_TEST_POSTGRES")))
                Skip = "Set ORDERHUB_CATEGORY_TEST_POSTGRES to a disposable PostgreSQL test server.";
        }
    }

    private sealed class ReadCounter : DbCommandInterceptor
    {
        public int Reads;
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref Reads);
            return ValueTask.FromResult(result);
        }
    }

    private sealed class PostgresFactory : AppDbContextFactory, IDisposable
    {
        private readonly string _adminConnection = Environment.GetEnvironmentVariable("ORDERHUB_CATEGORY_TEST_POSTGRES");
        private readonly string _database = "orderhub_category_test_" + Guid.NewGuid().ToString("N");
        private readonly string _connection;
        public readonly ReadCounter Counter = new();

        public PostgresFactory() : base("")
        {
            var builder = new NpgsqlConnectionStringBuilder(_adminConnection) { Database = _database, Pooling = false };
            _connection = builder.ConnectionString;
            using var admin = new NpgsqlConnection(_adminConnection);
            admin.Open();
            using var create = new NpgsqlCommand($"CREATE DATABASE \"{_database}\"", admin);
            create.ExecuteNonQuery();
            try
            {
                using var db = CreateDbContext();
                db.Database.EnsureCreated();
            }
            catch { Dispose(); throw; }
        }

        public override AppDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_connection).UseSnakeCaseNamingConvention().AddInterceptors(Counter).Options);

        public void Dispose()
        {
            using var admin = new NpgsqlConnection(_adminConnection);
            admin.Open();
            using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_database}\" WITH (FORCE)", admin);
            drop.ExecuteNonQuery();
        }
    }
}
