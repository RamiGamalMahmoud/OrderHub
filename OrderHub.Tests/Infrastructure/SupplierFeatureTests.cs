using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Features.Setup.Suppliers;
using OrderHub.Application.Features.Setup.Suppliers.Contracts;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using OrderHub.Infrastructure.Migrations;

namespace OrderHub.Tests.Infrastructure;

public class SupplierFeatureTests
{
    [Fact]
    public async Task Supplier_list_orders_projects_optional_data_and_returns_null_for_missing_target()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        Assert.Empty(await Execute(provider, new GetAllSuppliersQuery()));

        using (var db = factory.CreateDbContext())
        {
            db.Suppliers.AddRange(new Supplier("Zulu"), new Supplier("Alpha"));
            await db.SaveChangesAsync();
        }

        var list = (await Execute(provider, new GetAllSuppliersQuery())).ToList();
        Assert.Equal(new[] { "Alpha", "Zulu" }, list.Select(item => item.Name));
        Assert.Equal("", list[0].Address);
        Assert.Equal("", list[0].PhoneNumber);
        Assert.Null((await Execute(provider, new GetSupplierForEditQuery(list[0].Id))).OpenAt);
        Assert.Null(await Execute(provider, new GetSupplierForEditQuery(int.MaxValue)));
    }

    [Fact]
    public async Task Supplier_create_update_keep_contact_identity_and_existing_duplicate_semantics()
    {
        using var factory = TestDb.CreateFactory();
        int cityId = await City(factory);
        using var provider = SetupPersistenceTests.Services(factory);
        Assert.True((await Execute(provider, new CreateSupplierCommand(Input(cityId) with { Name = " Supplier " }))).IsSuccess);
        var item = Assert.Single(await Execute(provider, new GetAllSuppliersQuery()));
        Assert.Equal("Supplier", item.Name);
        Assert.Contains("City", item.Address);
        int addressId;
        int phoneId;

        using (var db = factory.CreateDbContext())
        {
            var entity = await db.Suppliers.Include(supplier => supplier.Address).Include(supplier => supplier.Phone).SingleAsync();
            addressId = entity.Address.Id;
            phoneId = entity.Phone.Id;
        }

        var update = new SupplierUpdateInput(item.Id, "Renamed", new(10, 0), new(18, 0), "New street", cityId, "01098765432", "+20");
        Assert.True((await Execute(provider, new UpdateSupplierCommand(update))).IsSuccess);
        var read = await Execute(provider, new GetSupplierForEditQuery(item.Id));
        Assert.Equal("Renamed", read.Name);
        Assert.Equal(new TimeOnly(18, 0), read.CloseAt);

        using (var db = factory.CreateDbContext())
        {
            var entity = await db.Suppliers.Include(supplier => supplier.Address).Include(supplier => supplier.Phone).SingleAsync();
            Assert.Equal(addressId, entity.Address.Id);
            Assert.Equal(phoneId, entity.Phone.Id);
        }

        Assert.True((await Execute(provider, new CreateSupplierCommand(Input(cityId)))).IsSuccess);
        Assert.True((await Execute(provider, new CreateSupplierCommand(Input(cityId)))).IsSuccess);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("phone")]
    [InlineData("country")]
    [InlineData("address")]
    [InlineData("city")]
    [InlineData("hours")]
    public async Task Supplier_invalid_input_never_persists_a_partial_profile(string field)
    {
        using var factory = TestDb.CreateFactory();
        int cityId = await City(factory);
        using var provider = SetupPersistenceTests.Services(factory);
        var input = Input(cityId);
        input = field switch
        {
            "name" => input with { Name = " " },
            "phone" => input with { PhoneNumber = "bad" },
            "country" => input with { CountryCode = "+1234" },
            "address" => input with { Street = "" },
            "city" => input with { CityId = int.MaxValue },
            _ => input with { OpenAt = new(20, 0), CloseAt = new(10, 0) }
        };

        Assert.False((await Execute(provider, new CreateSupplierCommand(input))).IsSuccess);
        Assert.Empty(await Execute(provider, new GetAllSuppliersQuery()));

        Assert.True((await Execute(provider, new CreateSupplierCommand(Input(cityId)))).IsSuccess);
        int id = Assert.Single(await Execute(provider, new GetAllSuppliersQuery())).Id;
        var update = new SupplierUpdateInput(
            id,
            input.Name,
            input.OpenAt,
            input.CloseAt,
            input.Street,
            input.CityId,
            input.PhoneNumber,
            input.CountryCode);

        Assert.False((await Execute(provider, new UpdateSupplierCommand(update))).IsSuccess);
        var unchanged = await Execute(provider, new GetSupplierForEditQuery(id));
        Assert.Equal("Supplier", unchanged.Name);
        Assert.Equal("Street", unchanged.Street);
        Assert.Equal("01012345678", unchanged.PhoneNumber);
        Assert.False((await Execute(provider, new UpdateSupplierCommand(new(int.MaxValue, "Missing", new(9, 0), new(17, 0), "Street", cityId, "01012345678", "+20")))).IsSuccess);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Supplier_product_and_order_references_block_deletion_without_touching_history(bool orderReference)
    {
        using var factory = TestDb.CreateFactory();
        int cityId = await City(factory);
        using var provider = SetupPersistenceTests.Services(factory);
        await Execute(provider, new CreateSupplierCommand(Input(cityId)));
        int id = Assert.Single(await Execute(provider, new GetAllSuppliersQuery())).Id;

        using (var db = factory.CreateDbContext())
        {
            var supplier = await db.Suppliers.FindAsync(id);
            var product = new Product("Product", "P1", new Category("Category"), new Money(10));

            if (!orderReference)
            {
                product.AddSupplier(supplier);
            }

            db.Products.Add(product);
            var client = new Client("Client", "Street", await db.Cities.FindAsync(cityId), "01012345678", "+20");
            db.Clients.Add(client);
            await db.SaveChangesAsync();

            if (orderReference)
            {
                var order = new Order(client.Id, "Supplier-order");
                order.AddOrderItem(product.Id, "Product", 10, 1, 0, "Supplier snapshot", id, []);
                db.Orders.Add(order);
                await db.SaveChangesAsync();
            }
        }

        Assert.False((await Execute(provider, new DeleteSupplierCommand(id))).IsSuccess);

        using (var directDelete = factory.CreateDbContext())
        {
            await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() =>
                directDelete.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM suppliers WHERE id = {id}"));
        }

        Assert.True((await Execute(provider, new UpdateSupplierCommand(new(
            id,
            "Renamed",
            new(9, 0),
            new(17, 0),
            "Street",
            cityId,
            "01012345678",
            "+20")))).IsSuccess);

        using var verify = factory.CreateDbContext();
        Assert.Single(verify.Products);
        Assert.Single(verify.Suppliers);

        if (orderReference)
        {
            Assert.Equal("Supplier snapshot", Assert.Single(verify.OrderItems).SupplierName);
        }
    }

    [Fact]
    public async Task Supplier_unused_delete_and_failed_contact_update_are_safe()
    {
        using var factory = TestDb.CreateFactory();
        int cityId = await City(factory);
        using var provider = SetupPersistenceTests.Services(factory);
        await Execute(provider, new CreateSupplierCommand(Input(cityId)));
        int id = Assert.Single(await Execute(provider, new GetAllSuppliersQuery())).Id;

        using (var db = factory.CreateDbContext())
        {
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_supplier_phone BEFORE UPDATE ON phones BEGIN SELECT RAISE(ABORT, 'Test failure'); END;");
        }

        var failed = await Execute(provider, new UpdateSupplierCommand(new(id, "Changed", new(10, 0), new(18, 0), "Changed street", cityId, "01098765432", "+20")));
        Assert.False(failed.IsSuccess);
        var read = await Execute(provider, new GetSupplierForEditQuery(id));
        Assert.Equal("Supplier", read.Name);
        Assert.Equal("Street", read.Street);
        Assert.Equal("01012345678", read.PhoneNumber);
        Assert.True((await Execute(provider, new DeleteSupplierCommand(id))).IsSuccess);
        Assert.False((await Execute(provider, new DeleteSupplierCommand(id))).IsSuccess);
    }

    [Fact]
    public async Task Supplier_name_only_profile_can_be_completed_without_replacing_the_supplier()
    {
        using var factory = TestDb.CreateFactory();
        int cityId = await City(factory);
        int id;

        using (var db = factory.CreateDbContext())
        {
            var supplier = new Supplier("Legacy");
            db.Suppliers.Add(supplier);
            await db.SaveChangesAsync();
            id = supplier.Id;
        }

        using var provider = SetupPersistenceTests.Services(factory);
        Assert.Null((await Execute(provider, new GetSupplierForEditQuery(id))).OpenAt);

        var update = new SupplierUpdateInput(
            id,
            "Legacy",
            new(9, 0),
            new(17, 0),
            "Street",
            cityId,
            "01012345678",
            "+20");

        Assert.True((await Execute(provider, new UpdateSupplierCommand(update))).IsSuccess);
        var completed = await Execute(provider, new GetSupplierForEditQuery(id));
        Assert.Equal(id, completed.Id);
        Assert.Equal(cityId, completed.CityId);
        Assert.Equal("Street", completed.Street);
        Assert.Equal("01012345678", completed.PhoneNumber);
        Assert.Equal(new TimeOnly(9, 0), completed.OpenAt);
        Assert.Single(await Execute(provider, new GetAllSuppliersQuery()));
    }

    [Fact]
    public void Supplier_migration_restricts_only_the_supplier_side_of_product_links()
    {
        var migration = new RestrictSupplierProductLinks();
        var add = Assert.Single(migration.UpOperations.OfType<AddForeignKeyOperation>());
        Assert.Equal(2, migration.UpOperations.Count);
        Assert.Equal("product_supplier", add.Table);
        Assert.Equal("suppliers", add.PrincipalTable);
        Assert.Equal(ReferentialAction.Restrict, add.OnDelete);
    }

    private static SupplierCreateInput Input(int cityId) => new("Supplier", new(9, 0), new(17, 0), "Street", cityId, "01012345678", "+20");

    private static async Task<int> City(TestDbFactory factory)
    {
        using var db = factory.CreateDbContext();
        var city = new City("City");
        db.Cities.Add(city);
        await db.SaveChangesAsync();
        return city.Id;
    }

    private static async Task<T> Execute<T>(ServiceProvider provider, IRequest<T> request)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request);
    }
}
