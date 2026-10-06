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
using OrderHub.Application.Features.Setup.Properties.Create;
using OrderHub.Application.Features.Setup.Properties.Get;
using OrderHub.Application.Features.Setup.Properties.Update;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Exceptions;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Features.Setup.Properties;

namespace OrderHub.Tests.Infrastructure;

public class PostgresPropertyTests
{
    [PropertyPostgresFact]
    public async Task PostgreSQL_counts_in_one_read_and_updates_option_identity_atomically()
    {
        using var factory = new PostgresFactory();
        using var provider = Services(factory);
        int id = await Execute(provider, new CreatePropertyCommand("Color", PropertyType.List, "", [new(null, "Red"), new(null, "Blue")]));
        factory.Counter.Reads = 0;
        var reader = new PropertyReader(factory);
        var item = Assert.Single(await reader.GetAllAsync(default));

        Assert.Equal(1, factory.Counter.Reads);
        Assert.Equal(2, item.OptionsCount);
        var original = await Execute(provider, new GetPropertyQuery(id));
        int retainedId = original.Options.First().Id;
        await Send(provider, new UpdatePropertyCommand(new(id, "Renamed", "", PropertyType.List,
            [new(retainedId, "Crimson"), new(null, "Green")])));
        var updated = await Execute(provider, new GetPropertyQuery(id));

        Assert.Equal(retainedId, updated.Options.First().Id);
        Assert.Equal("Crimson", updated.Options.First().Value);
        await Send(provider, new UpdatePropertyCommand(new(id, "Renamed", "", PropertyType.List,
            [new(retainedId, "Green"), new(updated.Options.Last().Id, "Crimson")])));
        var swapped = await Execute(provider, new GetPropertyQuery(id));

        Assert.Equal("Green", swapped.Options.First().Value);
        Assert.Equal(retainedId, swapped.Options.First().Id);
        await Assert.ThrowsAsync<DomainException>(() => Send(provider,
            new UpdatePropertyCommand(new(id, "Partial", "", PropertyType.List, [new(retainedId, " ")]))));
        Assert.Equal("Renamed", (await Execute(provider, new GetPropertyQuery(id))).Name);
    }

    [PropertyPostgresFact]
    public async Task PostgreSQL_restricts_order_references_instead_of_cascading_history()
    {
        using var factory = new PostgresFactory();
        using var provider = Services(factory);
        int id = await Execute(provider, new CreatePropertyCommand("Color", PropertyType.List, "", [new(null, "Red")]));

        using (var db = factory.CreateDbContext())
        {
            var product = new Product("Product", "P1", new Category("Category"), new Money(10));
            var client = new Client("Client", "Street", new City("City"), "01012345678", "+20");
            db.Products.Add(product);
            db.Clients.Add(client);
            await db.SaveChangesAsync();
            var order = new Order(client.Id, "Property-PG");
            order.AddOrderItem(product.Id, "Product", 10, 1, 0, null, null, [new(id, "Red")]);
            db.Orders.Add(order);
            await db.SaveChangesAsync();
        }

        var original = await Execute(provider, new GetPropertyQuery(id));
        await Assert.ThrowsAsync<DomainException>(() => Send(provider,
            new UpdatePropertyCommand(new(id, "Changed", "", PropertyType.List, [new(original.Options.First().Id, "Crimson")]))));

        using (var db = factory.CreateDbContext())
        {
            db.Properties.Remove(await db.Properties.FindAsync(id));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        using var verify = factory.CreateDbContext();

        Assert.Equal("Red", Assert.Single(verify.OrderItemProperties).Value);
    }

    private static async Task Send(ServiceProvider provider, IRequest request)
    {
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request);
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
    public sealed class PropertyPostgresFactAttribute : FactAttribute
    {
        public PropertyPostgresFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ORDERHUB_PROPERTY_TEST_POSTGRES")))
                Skip = "Set ORDERHUB_PROPERTY_TEST_POSTGRES to a disposable PostgreSQL test server.";
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
        private readonly string _adminConnection = Environment.GetEnvironmentVariable("ORDERHUB_PROPERTY_TEST_POSTGRES");
        private readonly string _database = "orderhub_property_test_" + Guid.NewGuid().ToString("N");
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
            catch
            {
                Dispose();
                throw;
            }
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

