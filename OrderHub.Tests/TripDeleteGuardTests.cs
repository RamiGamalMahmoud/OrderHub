using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Clients;
using OrderHub.Application.Features.Setup.Deliverymen;
using OrderHub.Application.Features.Setup.ShippingCarriers;
using OrderHub.Application.Features.Setup.Suppliers;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Models.Trips;
using OrderHub.Infrastructure.Features.Trips;

namespace OrderHub.Tests;

public sealed class TripDeleteGuardTests
{
    [Fact]
    public async Task Deliveryman_with_trips_cannot_be_deleted()
    {
        using var db = new TripTestDb();
        db.PersistTrip(1);
        var handler = DeliverymanHandler(db);

        var result = await handler.Handle(new DeleteDeliverymanCommand(db.Deliveryman.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Conflict", result.ErrorCode);
        Assert.Contains("لا يمكن حذف المندوب لارتباطه بمشاوير مستقلة.", result.ErrorMessage);
        Assert.True(await db.Context.Deliverymen.AnyAsync(deliveryman => deliveryman.Id == db.Deliveryman.Id));
    }

    [Fact]
    public async Task Deliveryman_referenced_only_by_history_cannot_be_deleted()
    {
        using var db = new TripTestDb();
        var trip = db.PersistTrip(1);
        var loaded = db.ReloadWithDeliveryman(trip.Id);
        loaded.Update(
            loaded.Title,
            loaded.Description,
            loaded.From,
            loaded.To,
            loaded.ScheduledAtUtc,
            db.OtherDeliveryman.Id,
            "ناصر",
            77,
            "نقل",
            db.Operation());
        db.Context.SaveChanges();

        var result = await DeliverymanHandler(db)
            .Handle(new DeleteDeliverymanCommand(db.Deliveryman.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Conflict", result.ErrorCode);
    }

    [Fact]
    public async Task Existing_order_guard_takes_precedence_over_trip_guard()
    {
        using var db = new TripTestDb();
        db.AddDirectOrderTrip(db.Deliveryman, 1, "ORD-GUARD");

        var result = await DeliverymanHandler(db)
            .Handle(new DeleteDeliverymanCommand(db.Deliveryman.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("مرتبط بطلبات", result.ErrorMessage);
        Assert.DoesNotContain("مشاوير مستقلة", result.ErrorMessage);
    }

    [Fact]
    public async Task Client_referenced_by_a_trip_cannot_be_deleted()
    {
        using var db = new TripTestDb();
        db.PersistTrip(1, from: TripParty.FromClient(db.Client));
        var context = new TripHandlerTestContext(db);
        var handler = new DeleteClientCommandHandler(
            context.Clients,
            context.UnitOfWork,
            new TripUsageReader(db.Context));

        var result = await handler.Handle(new DeleteClientCommand(db.Client.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Conflict", result.ErrorCode);
        Assert.Contains("لا يمكن حذف العميل لارتباطه بمشاوير مستقلة.", result.ErrorMessage);
    }

    [Fact]
    public async Task Supplier_referenced_by_a_trip_cannot_be_deleted()
    {
        using var db = new TripTestDb();
        db.PersistTrip(1, to: TripParty.FromSupplier(db.Supplier));
        var context = new TripHandlerTestContext(db);
        var handler = new DeleteSupplierCommandHandler(
            context.Suppliers,
            context.UnitOfWork,
            new TripUsageReader(db.Context));

        var result = await handler.Handle(new DeleteSupplierCommand(db.Supplier.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Conflict", result.ErrorCode);
        Assert.Contains("لا يمكن حذف المورد لارتباطه بمشاوير مستقلة.", result.ErrorMessage);
    }

    [Fact]
    public async Task Shipping_carrier_referenced_by_a_trip_cannot_be_deleted()
    {
        using var db = new TripTestDb();
        db.PersistTrip(1, to: TripParty.FromShippingCarrier(db.Carrier));
        var context = new TripHandlerTestContext(db);
        var handler = new DeleteShippingCarrierCommandHandler(
            context.Carriers,
            context.UnitOfWork,
            new TripUsageReader(db.Context));

        var result = await handler.Handle(
            new DeleteShippingCarrierCommand(db.Carrier.Id),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Conflict", result.ErrorCode);
        Assert.Contains("لا يمكن حذف شركة الشحن لارتباطها بمشاوير مستقلة.", result.ErrorMessage);
    }

    [Fact]
    public async Task Unreferenced_client_can_still_be_deleted()
    {
        using var db = new TripTestDb();
        db.PersistTrip(1, from: TripParty.FromClient(db.Client));
        var context = new TripHandlerTestContext(db);
        var handler = new DeleteClientCommandHandler(
            context.Clients,
            context.UnitOfWork,
            new TripUsageReader(db.Context));

        var result = await handler.Handle(new DeleteClientCommand(db.SecondClient.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(await db.Context.Clients.AnyAsync(client => client.Id == db.SecondClient.Id));
    }

    [Fact]
    public void Setup_handlers_never_depend_on_trip_feature_types()
    {
        var assembly = typeof(DeleteClientCommand).Assembly;
        var setupHandlers = assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith("OrderHub.Application.Features.Setup") == true)
            .Where(type => type.Name.EndsWith("Handler"))
            .ToArray();

        Assert.NotEmpty(setupHandlers);

        foreach (var handler in setupHandlers)
        {
            foreach (var parameter in handler.GetConstructors().SelectMany(
                         constructor => constructor.GetParameters()))
            {
                Assert.False(
                    parameter.ParameterType.Namespace?.StartsWith("OrderHub.Application.Features.Trips") == true,
                    $"{handler.Name} depends on {parameter.ParameterType.Name}.");
            }
        }

        var deleteClientDependencies = typeof(DeleteClientCommandHandler)
            .GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .ToArray();
        Assert.Contains(typeof(ITripUsageReader), deleteClientDependencies);
        Assert.DoesNotContain(typeof(IStandaloneTripRepository), deleteClientDependencies);
        Assert.DoesNotContain(typeof(ITripReader), deleteClientDependencies);
    }

    private static DeleteDeliverymanCommandHandler DeliverymanHandler(TripTestDb db)
    {
        var context = new TripHandlerTestContext(db);
        return new DeleteDeliverymanCommandHandler(
            context.Deliverymen,
            context.UnitOfWork,
            new TripUsageReader(db.Context));
    }
}
