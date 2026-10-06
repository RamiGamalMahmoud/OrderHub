using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Features.Setup.Deliverymen;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;

namespace OrderHub.Tests.Infrastructure;

public class DeliverymanFeatureTests
{
    [Fact]
    public async Task Deliveryman_list_and_optional_phone_preserve_domain_semantics()
    {
        using var factory = TestDb.CreateFactory();
        int cityId = await City(factory);
        using var provider = SetupPersistenceTests.Services(factory);
        Assert.Empty(await Execute(provider, new GetAllDeliverymenListQuery()));
        Assert.True((await Execute(provider, new CreateDeliverymanCommand(new(" Zulu ", cityId, null)))).IsSuccess);
        Assert.True((await Execute(provider, new CreateDeliverymanCommand(new("Alpha", cityId, "extension 123")))).IsSuccess);
        var list = (await Execute(provider, new GetAllDeliverymenListQuery())).ToList();
        Assert.Equal(new[] { "Alpha", "Zulu" }, list.Select(item => item.Name));
        Assert.Equal("City", list[0].CityName);
        Assert.Null(list[1].PhoneNumber);
        Assert.Null(await Execute(provider, new GetDeliverymanForEditQuery(int.MaxValue)));
        Assert.True((await Execute(provider, new CreateDeliverymanCommand(new("Alpha", cityId, "extension 123")))).IsSuccess);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Deliveryman_invalid_create_and_update_fail_without_mutation(int invalid)
    {
        using var factory = TestDb.CreateFactory();
        int cityId = await City(factory);
        using var provider = SetupPersistenceTests.Services(factory);
        var input = invalid switch
        {
            0 => new OrderHub.Application.Features.Setup.Deliverymen.Contracts.DeliverymanInput(" ", cityId, "123"),
            1 => new("Valid", int.MaxValue, "123"),
            _ => new("Valid", cityId, new string('x', 16))
        };
        Assert.False((await Execute(provider, new CreateDeliverymanCommand(input))).IsSuccess);
        Assert.True((await Execute(provider, new CreateDeliverymanCommand(new("Original", cityId, "123")))).IsSuccess);
        int id = Assert.Single(await Execute(provider, new GetAllDeliverymenListQuery())).Id;
        Assert.False((await Execute(provider, new UpdateDeliverymanCommand(id, input))).IsSuccess);
        Assert.Equal("Original", (await Execute(provider, new GetDeliverymanForEditQuery(id))).Name);
        Assert.False((await Execute(provider, new UpdateDeliverymanCommand(int.MaxValue, new("Valid", cityId, "123")))).IsSuccess);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Deliveryman_order_and_delivery_step_guards_preserve_operational_records(bool step)
    {
        using var factory = TestDb.CreateFactory();
        int cityId = await City(factory);
        using var provider = SetupPersistenceTests.Services(factory);
        await Execute(provider, new CreateDeliverymanCommand(new("Courier", cityId, "123")));
        int id = Assert.Single(await Execute(provider, new GetAllDeliverymenListQuery())).Id;

        using (var db = factory.CreateDbContext())
        {
            var client = new Client("Client", "Street", await db.Cities.FindAsync(cityId), "01012345678", "+20");
            db.Clients.Add(client);
            await db.SaveChangesAsync();
            var order = new Order(client.Id, "Delivery-order");

            if (step)
            {
                order.AddDeliveryStep(new() { StepOrder = 1, DeliveryMethod = DeliveryMethod.DeliveryMan, DeliverymanId = id });
            }
            else
            {
                order.UpdateHeader(client.Id, OrderSource.Online, DeliveryMethod.DeliveryMan, id, null, null);
            }

            db.Orders.Add(order);
            await db.SaveChangesAsync();
        }

        Assert.True((await Execute(provider, new UpdateDeliverymanCommand(id, new("Renamed", cityId, "456")))).IsSuccess);
        Assert.False((await Execute(provider, new DeleteDeliverymanCommand(id))).IsSuccess);
        using var verify = factory.CreateDbContext();
        Assert.Single(verify.Orders);
        Assert.Equal(id, step ? Assert.Single(verify.OrderDeliverySteps).DeliverymanId : Assert.Single(verify.Orders).DeliverymanId);
    }

    [Fact]
    public async Task Deliveryman_unused_delete_and_missing_delete_are_explicit()
    {
        using var factory = TestDb.CreateFactory();
        int cityId = await City(factory);
        using var provider = SetupPersistenceTests.Services(factory);
        await Execute(provider, new CreateDeliverymanCommand(new("Courier", cityId, "")));
        int id = Assert.Single(await Execute(provider, new GetAllDeliverymenListQuery())).Id;
        Assert.True((await Execute(provider, new DeleteDeliverymanCommand(id))).IsSuccess);
        Assert.False((await Execute(provider, new DeleteDeliverymanCommand(id))).IsSuccess);
    }

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
