using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Features.Setup.ShippingCarriers;
using OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;

namespace OrderHub.Tests.Infrastructure;

public class ShippingCarrierFeatureTests
{
    [Fact]
    public async Task ShippingCarrier_list_is_sorted_and_reads_contact_and_cost_projections()
    {
        using var factory = TestDb.CreateFactory();
        int cityId = await City(factory);
        using var provider = SetupPersistenceTests.Services(factory);
        Assert.Empty(await Execute(provider, new GetShippingCarriersQuery()));
        await Execute(provider, new CreateShippingCarrierCommand(Input(cityId) with { Name = "Zulu" }));
        await Execute(provider, new CreateShippingCarrierCommand(Input(cityId) with { Name = " Alpha " }));
        var list = (await Execute(provider, new GetShippingCarriersQuery())).ToList();
        Assert.Equal(new[] { "Alpha", "Zulu" }, list.Select(item => item.Name));
        Assert.Equal(10m, list[0].ShippingCost);
        Assert.Contains("City", list[0].Address);
        Assert.Contains("01012345678", list[0].PhoneNumber);
        Assert.Null(await Execute(provider, new GetShippingCarrierForEditQuery(int.MaxValue)));
        Assert.True((await Execute(provider, new CreateShippingCarrierCommand(Input(cityId)))).IsSuccess);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("phone")]
    [InlineData("address")]
    [InlineData("city")]
    [InlineData("cost")]
    public async Task ShippingCarrier_invalid_create_and_update_do_not_partially_mutate(string field)
    {
        using var factory = TestDb.CreateFactory();
        int cityId = await City(factory);
        using var provider = SetupPersistenceTests.Services(factory);
        var input = Input(cityId);
        input = field switch
        {
            "name" => input with { Name = " " },
            "phone" => input with { PhoneNumber = "bad" },
            "address" => input with { Street = "" },
            "city" => input with { CityId = int.MaxValue },
            _ => input with { ShippingCost = -1 }
        };

        Assert.False((await Execute(provider, new CreateShippingCarrierCommand(input))).IsSuccess);
        Assert.True((await Execute(provider, new CreateShippingCarrierCommand(Input(cityId)))).IsSuccess);
        int id = Assert.Single(await Execute(provider, new GetShippingCarriersQuery())).Id;
        var update = new ShippingCarrierUpdateInput(id, input.Name, input.ShippingCost, input.CountryCode, input.PhoneNumber, input.CityId, input.Street);
        Assert.False((await Execute(provider, new UpdateShippingCarrierCommand(update))).IsSuccess);
        Assert.Equal("Carrier", (await Execute(provider, new GetShippingCarrierForEditQuery(id))).Name);
        Assert.False((await Execute(provider, new UpdateShippingCarrierCommand(update with { Id = int.MaxValue }))).IsSuccess);
    }

    [Fact]
    public async Task ShippingCarrier_update_preserves_address_and_phone_ids_and_save_failure_is_atomic()
    {
        using var factory = TestDb.CreateFactory();
        int cityId = await City(factory);
        using var provider = SetupPersistenceTests.Services(factory);
        await Execute(provider, new CreateShippingCarrierCommand(Input(cityId)));
        int id = Assert.Single(await Execute(provider, new GetShippingCarriersQuery())).Id;
        int addressId;
        int phoneId;

        using (var db = factory.CreateDbContext())
        {
            var carrier = await db.ShippingCarriers.Include(item => item.Address).Include(item => item.Phone).SingleAsync();
            addressId = carrier.Address.Id;
            phoneId = carrier.Phone.Id;
        }

        var update = new ShippingCarrierUpdateInput(id, "Updated", 20, "+20", "01098765432", cityId, "New street");
        Assert.True((await Execute(provider, new UpdateShippingCarrierCommand(update))).IsSuccess);

        using (var db = factory.CreateDbContext())
        {
            var carrier = await db.ShippingCarriers.Include(item => item.Address).Include(item => item.Phone).SingleAsync();
            Assert.Equal(addressId, carrier.Address.Id);
            Assert.Equal(phoneId, carrier.Phone.Id);
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_carrier_phone BEFORE UPDATE ON phones BEGIN SELECT RAISE(ABORT, 'Test failure'); END;");
        }

        Assert.False((await Execute(provider, new UpdateShippingCarrierCommand(update with { Name = "Partial", PhoneNumber = "01011111111", ShippingCost = 30 }))).IsSuccess);
        var after = await Execute(provider, new GetShippingCarrierForEditQuery(id));
        Assert.Equal("Updated", after.Name);
        Assert.Equal(20m, after.ShippingCost);
        Assert.Equal("New street", after.Street);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ShippingCarrier_order_and_delivery_step_guards_preserve_history(bool step)
    {
        using var factory = TestDb.CreateFactory();
        int cityId = await City(factory);
        using var provider = SetupPersistenceTests.Services(factory);
        await Execute(provider, new CreateShippingCarrierCommand(Input(cityId)));
        int id = Assert.Single(await Execute(provider, new GetShippingCarriersQuery())).Id;

        using (var db = factory.CreateDbContext())
        {
            var client = new Client("Client", "Street", await db.Cities.FindAsync(cityId), "01012345678", "+20");
            db.Clients.Add(client);
            await db.SaveChangesAsync();
            var order = new Order(client.Id, "Carrier-order");

            if (step)
            {
                order.AddDeliveryStep(new() { StepOrder = 1, DeliveryMethod = DeliveryMethod.ShippingCompany, ShippingCarrierId = id });
            }
            else
            {
                order.UpdateHeader(client.Id, OrderSource.Online, DeliveryMethod.ShippingCompany, null, id, null);
            }

            db.Orders.Add(order);
            await db.SaveChangesAsync();
        }

        Assert.True((await Execute(provider, new UpdateShippingCarrierCommand(new(id, "Renamed", 20, "+20", "01012345678", cityId, "Street")))).IsSuccess);
        Assert.False((await Execute(provider, new DeleteShippingCarrierCommand(id))).IsSuccess);
        using var verify = factory.CreateDbContext();
        Assert.Single(verify.Orders);
        Assert.Equal(id, step ? Assert.Single(verify.OrderDeliverySteps).ShippingCarrierId : Assert.Single(verify.Orders).ShippingCarrierId);
    }

    [Fact]
    public async Task ShippingCarrier_unused_delete_and_missing_delete_are_explicit()
    {
        using var factory = TestDb.CreateFactory();
        int cityId = await City(factory);
        using var provider = SetupPersistenceTests.Services(factory);
        await Execute(provider, new CreateShippingCarrierCommand(Input(cityId)));
        int id = Assert.Single(await Execute(provider, new GetShippingCarriersQuery())).Id;
        Assert.True((await Execute(provider, new DeleteShippingCarrierCommand(id))).IsSuccess);
        Assert.False((await Execute(provider, new DeleteShippingCarrierCommand(id))).IsSuccess);
    }

    private static ShippingCarrierCreateInput Input(int cityId) => new("Carrier", 10, "+20", "01012345678", cityId, "Street");

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
