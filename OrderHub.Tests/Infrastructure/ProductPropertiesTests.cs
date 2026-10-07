using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Features.Setup.Properties.Create;
using OrderHub.Application.Features.Setup.Properties.Get;
using OrderHub.Application.Features.Setup.Properties.GetAll;
using OrderHub.Application.Features.Setup.Properties.Update;
using OrderHub.Application.Features.Setup.Properties.Delete;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Exceptions;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using OrderHub.Infrastructure.Migrations;

namespace OrderHub.Tests.Infrastructure;

public class ProductPropertiesTests
{
    [Fact]
    public async Task Index_is_empty_then_sorted_and_counts_owned_options()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);

        Assert.Empty(await Query(provider, new GetPropertiesQuery()));
        await Query(provider, new CreatePropertyCommand("Zulu", PropertyType.Text, "", []));
        int id = await Query(provider, new CreatePropertyCommand("Alpha", PropertyType.List, "", [new(null, "Red"), new(null, "Blue")]));
        var list = (await Query(provider, new GetPropertiesQuery())).ToList();

        Assert.Equal(new[] { "Alpha", "Zulu" }, list.Select(property => property.Name));
        Assert.Equal(2, list[0].OptionsCount);
        Assert.Equal(0, list[1].OptionsCount);
        Assert.Equal(2, (await Query(provider, new GetPropertyQuery(id))).Options.Count);
        Assert.Null(await Query(provider, new GetPropertyQuery(int.MaxValue)));
    }

    [Fact]
    public async Task Names_are_unique_after_trim_and_unchanged_edits_do_not_collide()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        int id = await Query(provider, new CreatePropertyCommand(" Color ", PropertyType.Text, "", []));
        await Query(provider, new CreatePropertyCommand("Size", PropertyType.Text, "", []));

        await Assert.ThrowsAsync<DomainException>(() => Query(provider,
            new CreatePropertyCommand(" Color ", PropertyType.Text, "", [])));
        await Assert.ThrowsAsync<DomainException>(() => Send(provider,
            new UpdatePropertyCommand(new(id, "Size", "", PropertyType.Text, []))));
        await Send(provider, new UpdatePropertyCommand(new(id, "Color", "", PropertyType.Text, [])));
        await Send(provider, new UpdatePropertyCommand(new(id, " Renamed ", " Description ", PropertyType.Text, [])));

        var updated = await Query(provider, new GetPropertyQuery(id));

        Assert.Equal("Renamed", updated.Name);
        Assert.Equal("Description", updated.Description);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task Invalid_creation_does_not_persist_any_aggregate_rows(int invalidCase)
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        var command = invalidCase switch
        {
            0 => new CreatePropertyCommand(" ", PropertyType.Text, "", []),
            1 => new CreatePropertyCommand("Color", PropertyType.List, "", []),
            2 => new CreatePropertyCommand("Color", PropertyType.List, "", [new(null, " ")]),
            3 => new CreatePropertyCommand("Color", PropertyType.List, "", [new(null, "Red"), new(null, " red ")]),
            _ => new CreatePropertyCommand(new string('x', 101), (PropertyType)99, "", [])
        };

        await Assert.ThrowsAsync<DomainException>(() => Query(provider, command));
        Assert.Empty(await Query(provider, new GetPropertiesQuery()));
    }

    [Fact]
    public async Task Option_diff_preserves_ids_and_invalid_updates_are_atomic()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        int id = await Query(provider, new CreatePropertyCommand("Color", PropertyType.List, "Original", [new(null, "Red"), new(null, "Blue")]));
        var original = await Query(provider, new GetPropertyQuery(id));
        var retained = original.Options.First();
        var removed = original.Options.Last();

        PropertyOptionUpdate[][] invalid =
        [
            [new(retained.Id, "Same"), new(null, " same ")],
            [new(int.MaxValue, "Foreign")],
            [new(retained.Id, "First"), new(retained.Id, "Repeated")],
            [],
            [new(retained.Id, new string('x', 101))]
        ];

        foreach (var options in invalid)
        {
            await Assert.ThrowsAsync<DomainException>(() => Send(provider,
                new UpdatePropertyCommand(new(id, "Changed", "Changed", PropertyType.List, options))));
        }

        var afterFailure = await Query(provider, new GetPropertyQuery(id));

        Assert.Equal("Color", afterFailure.Name);
        Assert.Equal("Original", afterFailure.Description);
        Assert.Equal(original.Options, afterFailure.Options);

        await Send(provider, new UpdatePropertyCommand(new(id, "Renamed", "", PropertyType.List,
            [new(retained.Id, " Crimson "), new(null, "Green")])));
        var updated = await Query(provider, new GetPropertyQuery(id));

        Assert.Equal(retained.Id, updated.Options.First().Id);
        Assert.Equal("Crimson", updated.Options.First().Value);
        Assert.DoesNotContain(updated.Options, option => option.Id == removed.Id);
        Assert.Equal("Green", updated.Options.Last().Value);
        await Assert.ThrowsAsync<DomainException>(() => Send(provider,
            new UpdatePropertyCommand(new(int.MaxValue, "Missing", "", PropertyType.Text, []))));
    }

    [Fact]
    public async Task Unused_properties_can_change_type_and_delete_their_owned_options()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        int id = await Query(provider, new CreatePropertyCommand("Color", PropertyType.List, "", [new(null, "Red")]));

        await Send(provider, new UpdatePropertyCommand(new(id, "Notes", "", PropertyType.Text, [])));
        Assert.Empty((await Query(provider, new GetPropertyQuery(id))).Options);
        await Send(provider, new UpdatePropertyCommand(new(id, "List again", "", PropertyType.List, [new(null, "New")])));
        Assert.True((await Query(provider, new DeletePropertyCommand(id))).IsSuccess);
        Assert.False((await Query(provider, new DeletePropertyCommand(id))).IsSuccess);

        using var db = factory.CreateDbContext();

        Assert.Empty(db.PropertyOptions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Product_or_order_usage_blocks_destructive_definition_changes(bool orderUsage)
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        int id = await Query(provider, new CreatePropertyCommand("Color", PropertyType.List, "", [new(null, "Red"), new(null, "Blue")]));
        await AddUsage(factory, id, orderUsage);

        Assert.False((await Query(provider, new DeletePropertyCommand(id))).IsSuccess);
        await Assert.ThrowsAsync<DomainException>(() => Send(provider,
            new UpdatePropertyCommand(new(id, "Changed", "", PropertyType.Text, []))));

        if (orderUsage)
        {
            var original = await Query(provider, new GetPropertyQuery(id));
            var red = original.Options.Single(option => option.Value == "Red");
            var blue = original.Options.Single(option => option.Value == "Blue");

            await Assert.ThrowsAsync<DomainException>(() => Send(provider,
                new UpdatePropertyCommand(new(id, "Changed", "", PropertyType.List, [new(blue.Id, "Blue")]))));
            await Assert.ThrowsAsync<DomainException>(() => Send(provider,
                new UpdatePropertyCommand(new(id, "Changed", "", PropertyType.List,
                    [new(red.Id, "Crimson"), new(blue.Id, "Blue")]))));

            await Send(provider, new UpdatePropertyCommand(new(id, "Color", "", PropertyType.List,
                [new(red.Id, "Red"), new(blue.Id, "Green")])));

            using (var db = factory.CreateDbContext())
            {
                Assert.Equal("Red", Assert.Single(db.OrderItemProperties.AsNoTracking()).Value);
                db.Properties.Remove(await db.Properties.FindAsync(id));
                await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            }

            using var verify = factory.CreateDbContext();

            Assert.Single(verify.OrderItemProperties);
        }
    }

    [Fact]
    public async Task Persistence_failure_rolls_back_name_and_option_changes_together()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        int id = await Query(provider, new CreatePropertyCommand("Original", PropertyType.List, "", [new(null, "Red")]));
        var original = await Query(provider, new GetPropertyQuery(id));

        using (var db = factory.CreateDbContext())
        {
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TRIGGER reject_test_option BEFORE INSERT ON property_options
                WHEN NEW.value = 'Blocked'
                BEGIN SELECT RAISE(ABORT, 'Test option persistence failure'); END;
                """);
        }

        await Assert.ThrowsAsync<DbUpdateException>(() => Send(provider,
            new UpdatePropertyCommand(new(id, "Changed", "", PropertyType.List,
                [new(original.Options.First().Id, "Crimson"), new(null, "Blocked")]))));

        var after = await Query(provider, new GetPropertyQuery(id));

        Assert.Equal("Original", after.Name);
        Assert.Equal(original.Options, after.Options);
    }

    [Fact]
    public void Migration_changes_only_the_order_property_delete_action()
    {
        var migration = new RestrictPropertyDeletionForOrders();
        var add = Assert.Single(migration.UpOperations.OfType<AddForeignKeyOperation>(), operation => operation.Table == "order_item_properties");

        Assert.Equal(4, migration.UpOperations.Count);
        var role = Assert.Single(migration.UpOperations.OfType<AddForeignKeyOperation>(), operation => operation.Table == "users");
        Assert.Equal("roles", role.PrincipalTable);
        Assert.Equal(ReferentialAction.Restrict, role.OnDelete);
        Assert.Equal("order_item_properties", add.Table);
        Assert.Equal("properties", add.PrincipalTable);
        Assert.Equal(ReferentialAction.Restrict, add.OnDelete);
    }

    private static async Task AddUsage(TestDbFactory factory, int propertyId, bool orderUsage)
    {
        using var db = factory.CreateDbContext();
        var product = new Product("Product", "P1", new Category("Category"), new Money(10));
        db.Products.Add(product);

        if (!orderUsage)
        {
            product.AddProperty(propertyId, true);
            await db.SaveChangesAsync();
            return;
        }

        var client = new Client("Client", "Street", new City("City"), "01012345678", "+20");
        db.Clients.Add(client);
        await db.SaveChangesAsync();
        var order = new Order(client.Id, "Property-test");
        order.AddOrderItem(product.Id, "Product", 10, 1, 0, null, null, [new(propertyId, "Red")]);
        db.Orders.Add(order);
        await db.SaveChangesAsync();
    }

    private static async Task<T> Query<T>(ServiceProvider provider, IRequest<T> request)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request);
    }

    private static async Task Send(ServiceProvider provider, IRequest request)
    {
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request);
    }
}
