using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Features.Setup.Clients;
using OrderHub.Application.Features.Setup.Clients.Contracts;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Domain.Models;
using OrderHub.Domain.Models.CommercialDocuments;
using OrderHub.Infrastructure.Models;

namespace OrderHub.Tests.Infrastructure;

public class ClientFeatureTests
{
    [Fact]
    public async Task Reads_order_by_name_then_id_and_project_edit_lookup_and_missing_target()
    {
        using var factory = TestDb.CreateFactory();
        int city = await City(factory);
        using var provider = SetupPersistenceTests.Services(factory);
        Assert.Empty(await Execute(provider, new GetAllClientsQuery()));
        foreach (string name in new[] { "Zulu", "Alpha", "Alpha" })
        {
            Assert.True((await Execute(provider, new CreateClientCommand(Input(city) with { Name = name }))).IsSuccess);
        }

        var list = (await Execute(provider, new GetAllClientsQuery())).ToArray();
        Assert.Equal(new[] { "Alpha", "Alpha", "Zulu" }, list.Select(row => row.Name));
        Assert.True(list[0].Id < list[1].Id);
        Assert.Equal("City - Street", list[0].Address);
        Assert.Equal("+20 - 01012345678", list[0].PhoneNumber);
        Assert.Equal("Location", list[0].Location);
        Assert.Equal(city, (await Execute(provider, new GetClientEditQuery(list[0].Id))).CityId);
        Assert.Null(await Execute(provider, new GetClientEditQuery(int.MaxValue)));
        Assert.Null(await Execute(provider, new GetClientByIdQuery(int.MaxValue)));
        Assert.Single(await Execute(provider, new GetClientEditorCitiesQuery()));
        Assert.Equal(list.Select(row => row.Id), (await Execute(provider, new GetClientsByNameQuery("Street"))).Select(row => row.Id));
        Assert.Equal(list.Take(2).Select(row => row.Id), (await Execute(provider, new GetAllClientsPagedQuery(1, 2))).Items.Select(row => row.Id));
    }

    [Fact]
    public async Task Mutations_trim_preserve_exclusive_contact_ids_allow_duplicates_and_handle_missing()
    {
        using var factory = TestDb.CreateFactory();
        int city = await City(factory);
        using var provider = SetupPersistenceTests.Services(factory);
        var input = Input(city) with { Name = " Client ", Street = " Street ", PhoneNumber = " 01012345678 ", CountryCode = " +20 ", Location = " Location " };
        Assert.True((await Execute(provider, new CreateClientCommand(input))).IsSuccess);
        int id = Assert.Single(await Execute(provider, new GetAllClientsQuery())).Id;
        int addressId;
        int phoneId;
        using (var db = factory.CreateDbContext())
        {
            var client = await db.Clients.Include(row => row.Phone).Include(row => row.Address).SingleAsync();
            addressId = client.Address.Id;
            phoneId = client.Phone.Id;
        }

        Assert.True((await Execute(provider, new UpdateClientCommand(id, Input(city) with { Name = "Renamed", Street = "Changed", PhoneNumber = "123456", Location = " " }))).IsSuccess);
        var edit = await Execute(provider, new GetClientEditQuery(id));
        Assert.Equal("Renamed", edit.Name);
        Assert.Null(edit.Location);
        using (var db = factory.CreateDbContext())
        {
            var client = await db.Clients.Include(row => row.Phone).Include(row => row.Address).SingleAsync();
            Assert.Equal(addressId, client.Address.Id);
            Assert.Equal(phoneId, client.Phone.Id);
        }

        Assert.True((await Execute(provider, new CreateClientCommand(Input(city)))).IsSuccess);
        Assert.True((await Execute(provider, new CreateClientCommand(Input(city)))).IsSuccess);
        Assert.False((await Execute(provider, new UpdateClientCommand(int.MaxValue, Input(city)))).IsSuccess);
        Assert.True((await Execute(provider, new DeleteClientCommand(id))).IsSuccess);
        Assert.False((await Execute(provider, new DeleteClientCommand(id))).IsSuccess);
        using var verify = factory.CreateDbContext();
        Assert.Equal(3, await verify.Phones.CountAsync());
        Assert.Equal(3, await verify.Addresses.CountAsync());
    }

    [Theory]
    [InlineData("name")]
    [InlineData("phone")]
    [InlineData("country")]
    [InlineData("street")]
    [InlineData("city")]
    [InlineData("location")]
    [InlineData("null")]
    public async Task Invalid_create_and_update_never_persist_partial_changes(string field)
    {
        using var factory = TestDb.CreateFactory();
        int city = await City(factory);
        using var provider = SetupPersistenceTests.Services(factory);
        var bad = field switch
        {
            "name" => Input(city) with { Name = " x " },
            "phone" => Input(city) with { PhoneNumber = "bad" },
            "country" => Input(city) with { CountryCode = "+1234" },
            "street" => Input(city) with { Street = new string('s', 151) },
            "city" => Input(int.MaxValue),
            "location" => Input(city) with { Location = new string('x', 256) },
            _ => null
        };
        Assert.False((await Execute(provider, new CreateClientCommand(bad))).IsSuccess);
        Assert.Empty(await Execute(provider, new GetAllClientsQuery()));
        await Execute(provider, new CreateClientCommand(Input(city)));
        int id = Assert.Single(await Execute(provider, new GetAllClientsQuery())).Id;
        Assert.False((await Execute(provider, new UpdateClientCommand(id, bad))).IsSuccess);
        Assert.Equal(Input(city), await Execute(provider, new GetClientEditQuery(id)));
    }

    [Fact]
    public async Task Orders_block_business_and_direct_deletion_and_document_snapshots_survive_edits()
    {
        using var factory = TestDb.CreateFactory();
        int city = await City(factory);
        using var provider = SetupPersistenceTests.Services(factory);
        await Execute(provider, new CreateClientCommand(Input(city)));
        int id = Assert.Single(await Execute(provider, new GetAllClientsQuery())).Id;
        using (var db = factory.CreateDbContext())
        {
            var product = new Product("Product", "Client-P1", new Category("Category"), new OrderHub.Domain.ValueObjects.Money(10));
            db.Products.Add(product);
            await db.SaveChangesAsync();
            var order = new Order(id, "Client-order");
            order.AddOrderItem(product.Id, "Product snapshot", 10, 1, 0, null, null, []);
            order.AddDeliveryStep(new OrderDeliveryStep { StepOrder = 1, DeliveryMethod = OrderHub.Domain.Enums.DeliveryMethod.Pickup });
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            db.Invoices.Add(Invoice.Create("I-1", DateTime.UtcNow, "Historical name", "Historical phone", "Historical address", 10, 0, 10, order.Id));
            var proforma = ProformaInvoice.Create("P-1", DateTime.UtcNow, "Historical name", "Historical phone", "Historical address", 10, 0, 10, Guid.NewGuid());
            proforma.LinkToOrder(order.Id);
            db.ProformaInvoices.Add(proforma);
            db.Quotations.Add(Quotation.Create("Q-1", DateTime.UtcNow, "Historical name", "Historical phone", "Historical address", 0, 0, 0, DateTime.UtcNow.AddDays(1), Guid.NewGuid()));
            await db.SaveChangesAsync();
        }

        var result = await Execute(provider, new DeleteClientCommand(id));
        Assert.False(result.IsSuccess);
        Assert.Contains("طلبات", result.ErrorMessage);
        using (var db = factory.CreateDbContext())
        {
            await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM clients WHERE id = {id}"));
        }

        Assert.True((await Execute(provider, new UpdateClientCommand(id, Input(city) with { Name = "Renamed", Street = "Changed", PhoneNumber = "123456" }))).IsSuccess);
        using var verify = factory.CreateDbContext();
        Assert.Equal(id, Assert.Single(verify.Orders).ClientId);
        Assert.Equal("Product snapshot", Assert.Single(verify.OrderItems).ProductName);
        Assert.Single(verify.OrderDeliverySteps);
        Assert.Equal("Historical name", Assert.Single(verify.Invoices).CustomerName);
        Assert.Equal("Historical phone", Assert.Single(verify.Invoices).CustomerPhone);
        Assert.Equal("Historical address", Assert.Single(verify.Invoices).CustomerAddress);
        Assert.Equal("Historical name", Assert.Single(verify.ProformaInvoices).CustomerName);
        Assert.Equal("Historical phone", Assert.Single(verify.ProformaInvoices).CustomerPhone);
        Assert.Equal("Historical address", Assert.Single(verify.ProformaInvoices).CustomerAddress);
        var quotation = Assert.Single(verify.Quotations);
        Assert.Equal("Historical name", quotation.CustomerName);
        Assert.Equal("Historical phone", quotation.CustomerPhone);
        Assert.Equal("Historical address", quotation.CustomerAddress);
        var foreignKeys = verify.Model.FindEntityType(typeof(Client)).GetReferencingForeignKeys()
            .Where(fk => !fk.IsOwnership).ToArray();
        Assert.Single(foreignKeys);
        Assert.All(foreignKeys, fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));
    }

    [Fact]
    public async Task Draft_reference_blocks_deletion_and_is_not_rewritten_on_update()
    {
        using var factory = TestDb.CreateFactory();
        int city = await City(factory);
        using var provider = SetupPersistenceTests.Services(factory);
        await Execute(provider, new CreateClientCommand(Input(city)));
        int id = Assert.Single(await Execute(provider, new GetAllClientsQuery())).Id;
        string json = JsonSerializer.Serialize(new Draft(Guid.NewGuid(), 1, DateTime.UtcNow, new OrderDraftData(id, "Client snapshot", null, null, null, null, null, [], [])));
        using (var db = factory.CreateDbContext())
        {
            var user = User.Create("draft-owner", "hash", "Owner");
            db.Users.Add(user);
            await db.SaveChangesAsync();
            db.DraftRecords.Add(new DraftRecord { Id = Guid.NewGuid(), UserId = user.Id, Data = json, LastModified = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
        Assert.False((await Execute(provider, new DeleteClientCommand(id))).IsSuccess);
        Assert.True((await Execute(provider, new UpdateClientCommand(id, Input(city) with { Name = "Renamed" }))).IsSuccess);
        using var verify = factory.CreateDbContext();
        Assert.Equal(json, Assert.Single(verify.DraftRecords).Data);
    }

    [Fact]
    public async Task Shared_contacts_are_detached_only_when_changed_and_other_party_stays_intact()
    {
        using var factory = TestDb.CreateFactory();
        int city = await City(factory);
        using var provider = SetupPersistenceTests.Services(factory);
        await Execute(provider, new CreateClientCommand(Input(city)));
        int id = Assert.Single(await Execute(provider, new GetAllClientsQuery())).Id;
        int addressId;
        int phoneId;
        using (var db = factory.CreateDbContext())
        {
            var client = await db.Clients.Include(row => row.Phone).Include(row => row.Address).SingleAsync();
            addressId = client.Address.Id;
            phoneId = client.Phone.Id;
            db.Suppliers.Add(new Supplier("Shared supplier", new(9, 0), new(17, 0), client.Address, client.Phone));
            await db.SaveChangesAsync();
        }
        Assert.True((await Execute(provider, new UpdateClientCommand(id, Input(city) with { Name = "Only name" }))).IsSuccess);
        using (var db = factory.CreateDbContext())
        {
            Assert.Equal(phoneId, (await db.Clients.Include(row => row.Phone).SingleAsync()).Phone.Id);
        }
        Assert.True((await Execute(provider, new UpdateClientCommand(id, Input(city) with { Street = "Changed", PhoneNumber = "123456" }))).IsSuccess);
        using var verify = factory.CreateDbContext();
        var updated = await verify.Clients.Include(row => row.Phone).Include(row => row.Address).SingleAsync();
        var other = await verify.Suppliers.Include(row => row.Phone).Include(row => row.Address).SingleAsync();
        Assert.NotEqual(phoneId, updated.Phone.Id);
        Assert.NotEqual(addressId, updated.Address.Id);
        Assert.Equal(phoneId, other.Phone.Id);
        Assert.Equal(addressId, other.Address.Id);
        Assert.Equal("01012345678", other.Phone.Number.NationalNumber);
        Assert.Equal("Street", other.Address.Street);
    }

    [Fact]
    public async Task Database_failure_rolls_back_entire_contact_update()
    {
        using var factory = TestDb.CreateFactory();
        int city = await City(factory);
        using var provider = SetupPersistenceTests.Services(factory);
        await Execute(provider, new CreateClientCommand(Input(city)));
        int id = Assert.Single(await Execute(provider, new GetAllClientsQuery())).Id;
        using (var db = factory.CreateDbContext())
        {
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_client_phone BEFORE UPDATE ON phones BEGIN SELECT RAISE(ABORT, 'Test failure'); END;");
        }
        Assert.False((await Execute(provider, new UpdateClientCommand(id, Input(city) with { Name = "Changed", Street = "Changed", PhoneNumber = "123456" }))).IsSuccess);
        Assert.Equal(Input(city), await Execute(provider, new GetClientEditQuery(id)));
    }

    private static ClientInput Input(int city) => new("Client", "Street", city, "01012345678", "+20", "Location");

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
