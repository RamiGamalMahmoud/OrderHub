using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Services;

namespace OrderHub.Tests;

public sealed class WorkRecordNumberingTests
{
    [Fact]
    public async Task Supply_numbers_are_scoped_by_supplier_and_Riyadh_month_and_year()
    {
        PostgresTestDatabase.Reset();
        var options = PostgresTestDatabase.Options;
        using var context = new AppDbContext(options);
        var unitOfWork = CreateUnitOfWork(context, options);
        var (client, firstSupplier, secondSupplier) = SeedSupplyParents(context);

        var jan31 = await SaveSupplyAsync(context, unitOfWork, client, firstSupplier,
            new DateTime(2026, 1, 31, 20, 59, 0, DateTimeKind.Utc));
        var sameMonth = await SaveSupplyAsync(context, unitOfWork, client, firstSupplier,
            new DateTime(2026, 1, 31, 20, 30, 0, DateTimeKind.Utc));
        var feb1 = await SaveSupplyAsync(context, unitOfWork, client, firstSupplier,
            new DateTime(2026, 1, 31, 21, 0, 0, DateTimeKind.Utc));
        var anotherSupplier = await SaveSupplyAsync(context, unitOfWork, client, secondSupplier,
            new DateTime(2026, 1, 31, 21, 0, 0, DateTimeKind.Utc));
        var beforeRiyadhYearBoundary = await SaveSupplyAsync(context, unitOfWork, client, firstSupplier,
            new DateTime(2026, 12, 31, 20, 59, 0, DateTimeKind.Utc));
        var atRiyadhYearBoundary = await SaveSupplyAsync(context, unitOfWork, client, firstSupplier,
            new DateTime(2026, 12, 31, 21, 0, 0, DateTimeKind.Utc));
        var nextYear = await SaveSupplyAsync(context, unitOfWork, client, firstSupplier,
            new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal((1L, 2026, 1), (jan31.WorkRecordNumber, jan31.SequenceYear, jan31.SequenceMonth));
        Assert.Equal((2L, 2026, 1), (sameMonth.WorkRecordNumber, sameMonth.SequenceYear, sameMonth.SequenceMonth));
        Assert.Equal((1L, 2026, 2), (feb1.WorkRecordNumber, feb1.SequenceYear, feb1.SequenceMonth));
        Assert.Equal((1L, 2026, 2), (anotherSupplier.WorkRecordNumber, anotherSupplier.SequenceYear, anotherSupplier.SequenceMonth));
        Assert.Equal((1L, 2026, 12), (beforeRiyadhYearBoundary.WorkRecordNumber, beforeRiyadhYearBoundary.SequenceYear, beforeRiyadhYearBoundary.SequenceMonth));
        Assert.Equal((1L, 2027, 1), (atRiyadhYearBoundary.WorkRecordNumber, atRiyadhYearBoundary.SequenceYear, atRiyadhYearBoundary.SequenceMonth));
        Assert.Equal((2L, 2027, 1), (nextYear.WorkRecordNumber, nextYear.SequenceYear, nextYear.SequenceMonth));
    }

    [Fact]
    public async Task Delivery_trip_and_shipment_use_their_own_party_sequences()
    {
        PostgresTestDatabase.Reset();
        var options = PostgresTestDatabase.Options;
        using var context = new AppDbContext(options);
        var unitOfWork = CreateUnitOfWork(context, options);
        var (city, client, user) = SeedOrderParents(context);
        var firstDeliveryman = NewDeliveryman(context, city, "مندوب أول", "0500000001");
        var secondDeliveryman = NewDeliveryman(context, city, "مندوب ثان", "0500000002");
        var firstCarrier = NewCarrier(context, city, "شركة أولى");
        var secondCarrier = NewCarrier(context, city, "شركة ثانية");
        context.SaveChanges();

        var tripOrder = NewOrder(client, DeliveryMethod.DeliveryMan, deliverymanId: firstDeliveryman.Id);
        tripOrder.TransitionTo(OrderStatus.OutForDelivery, Operation(user));
        var shipmentOrder = NewOrder(client, DeliveryMethod.ShippingCompany, shippingCarrierId: firstCarrier.Id);
        shipmentOrder.TransitionTo(OrderStatus.Shipped, Operation(user));
        var otherCarrierOrder = NewOrder(client, DeliveryMethod.ShippingCompany, shippingCarrierId: secondCarrier.Id);
        otherCarrierOrder.TransitionTo(OrderStatus.Shipped, Operation(user));
        var secondTripOrder = NewOrder(client, DeliveryMethod.DeliveryMan, deliverymanId: secondDeliveryman.Id);
        secondTripOrder.TransitionTo(OrderStatus.OutForDelivery, Operation(user));
        context.Orders.AddRange(tripOrder, shipmentOrder, otherCarrierOrder, secondTripOrder);

        await unitOfWork.SaveChangesAsync();

        var trip = Assert.Single(tripOrder.DeliveryTrips);
        var shipment = Assert.Single(shipmentOrder.Shipments);
        var otherCarrierShipment = Assert.Single(otherCarrierOrder.Shipments);
        var otherPartyTrip = Assert.Single(secondTripOrder.DeliveryTrips);
        Assert.Equal((firstDeliveryman.Id, 1L), (((IWorkRecord)trip).NumberingPartyId, trip.WorkRecordNumber));
        Assert.Equal((firstCarrier.Id, 1L), (((IWorkRecord)shipment).NumberingPartyId, shipment.WorkRecordNumber));
        Assert.Equal((secondCarrier.Id, 1L), (((IWorkRecord)otherCarrierShipment).NumberingPartyId, otherCarrierShipment.WorkRecordNumber));
        Assert.Equal((secondDeliveryman.Id, 1L), (((IWorkRecord)otherPartyTrip).NumberingPartyId, otherPartyTrip.WorkRecordNumber));
    }

    [Fact]
    public async Task Failed_attempt_keeps_its_number_and_retry_gets_a_new_number()
    {
        PostgresTestDatabase.Reset();
        var options = PostgresTestDatabase.Options;
        using var context = new AppDbContext(options);
        var unitOfWork = CreateUnitOfWork(context, options);
        var (city, client, user) = SeedOrderParents(context);
        var deliveryman = NewDeliveryman(context, city, "مندوب", "0500000003");
        context.SaveChanges();

        var order = NewOrder(client, DeliveryMethod.DeliveryMan, deliverymanId: deliveryman.Id);
        order.TransitionTo(OrderStatus.OutForDelivery, Operation(user));
        context.Orders.Add(order);
        await unitOfWork.SaveChangesAsync();
        var failedAttempt = Assert.Single(order.DeliveryTrips);
        var originalNumber = failedAttempt.WorkRecordNumber;

        order.TransitionTo(OrderStatus.DeliveryFailed, Operation(user), "تعذر التسليم");
        await unitOfWork.SaveChangesAsync();
        order.TransitionTo(OrderStatus.OutForDelivery, Operation(user));
        await unitOfWork.SaveChangesAsync();

        Assert.Equal(WorkRecordStatus.Failed, failedAttempt.Status);
        Assert.Equal(originalNumber, failedAttempt.WorkRecordNumber);
        var retry = order.DeliveryTrips.Single(record => !ReferenceEquals(record, failedAttempt));
        Assert.Equal(2, retry.WorkRecordNumber);
        Assert.Equal(originalNumber, context.DeliveryTrips.Single(record => record.Id == failedAttempt.Id).WorkRecordNumber);
    }

    [Fact]
    public async Task Status_updates_and_queries_do_not_allocate_another_number()
    {
        PostgresTestDatabase.Reset();
        var options = PostgresTestDatabase.Options;
        using var context = new AppDbContext(options);
        var unitOfWork = CreateUnitOfWork(context, options);
        var (client, supplier) = SeedSingleSupplierParents(context);
        var supply = NewSupply(client, supplier, new DateTime(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc));
        context.Supplies.Add(supply);
        await unitOfWork.SaveChangesAsync();
        var number = supply.WorkRecordNumber;

        var cancelledSupply = NewSupply(client, supplier, new DateTime(2026, 10, 14, 12, 2, 0, DateTimeKind.Utc));
        context.Supplies.Add(cancelledSupply);
        await unitOfWork.SaveChangesAsync();

        supply.TransitionTo(WorkRecordStatus.Completed, atUtc: new DateTime(2026, 10, 14, 12, 1, 0, DateTimeKind.Utc));
        cancelledSupply.TransitionTo(WorkRecordStatus.Cancelled, "تم إلغاء الطلب", new DateTime(2026, 10, 14, 12, 3, 0, DateTimeKind.Utc));
        await unitOfWork.SaveChangesAsync();
        Assert.Equal(2, await context.Supplies.AsNoTracking().CountAsync());

        Assert.Equal(number, supply.WorkRecordNumber);
        Assert.Equal(2, cancelledSupply.WorkRecordNumber);
        var sequence = await context.NumberSequences.SingleAsync();
        Assert.Equal(2, sequence.LastNumber);
    }

    [Fact]
    public async Task Failed_save_rolls_back_record_and_number_sequence_together()
    {
        PostgresTestDatabase.Reset();
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(PostgresTestDatabase.ConnectionString)
            .UseSnakeCaseNamingConvention();
        var interceptor = new FailHistoryInsertInterceptor();
        optionsBuilder.AddInterceptors(interceptor);
        var options = optionsBuilder.Options;

        Client client;
        Supplier supplier;
        using (var seedContext = new AppDbContext(options))
        {
            (client, supplier) = SeedSingleSupplierParents(seedContext);
        }

        try
        {
            using (var context = new AppDbContext(options))
            {
                context.Supplies.Add(NewSupply(client, supplier, DateTime.UtcNow));
                interceptor.Enabled = true;
                await Assert.ThrowsAsync<PersistenceException>(() => CreateUnitOfWork(context, options).SaveChangesAsync());
            }
        }
        finally
        {
            interceptor.Enabled = false;
        }

        using var verification = new AppDbContext(options);
        Assert.Empty(await verification.Supplies.ToListAsync());
        Assert.Empty(await verification.NumberSequences.ToListAsync());
    }

    [Fact]
    public async Task Concurrent_allocations_are_unique_for_one_party_and_month()
    {
        PostgresTestDatabase.Reset();
        var options = PostgresTestDatabase.Options;
        var atUtc = new DateTimeOffset(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);
        var allocations = await Task.WhenAll(Enumerable.Range(0, 16).Select(async _ =>
        {
            await using var context = new AppDbContext(options);
            var service = new NumberSequenceService(context, new AppDbContextFactory(options));
            return await service.NextMonthlyAsync(NumberSequenceKind.Supplier, 77, atUtc);
        }));

        Assert.Equal(Enumerable.Range(1, 16).Select(number => (long)number).Order(), allocations.Select(x => x.Number).Order());
        Assert.All(allocations, allocation => Assert.Equal((2026, 10), (allocation.Year, allocation.Month)));
    }

    [Fact]
    public async Task Warehouse_kind_allocates_monthly_numbers_and_legacy_kinds_keep_working()
    {
        PostgresTestDatabase.Reset();
        var options = PostgresTestDatabase.Options;
        using var context = new AppDbContext(options);
        var service = new NumberSequenceService(context, new AppDbContextFactory(options));
        var atUtc = new DateTimeOffset(2026, 11, 13, 9, 0, 0, TimeSpan.Zero);

        foreach (var kind in new[]
                 {
                     NumberSequenceKind.Supplier,
                     NumberSequenceKind.Deliveryman,
                     NumberSequenceKind.ShippingCarrier,
                     NumberSequenceKind.Warehouse
                 })
        {
            var first = await service.NextMonthlyAsync(kind, 7, atUtc);
            var second = await service.NextMonthlyAsync(kind, 7, atUtc);
            Assert.Equal((1L, 2026, 11), (first.Number, first.Year, first.Month));
            Assert.Equal((2L, 2026, 11), (second.Number, second.Year, second.Month));
        }

        Assert.Equal(4, await context.NumberSequences.CountAsync());
        Assert.Contains(await context.NumberSequences.AsNoTracking().ToListAsync(),
            row => row.Kind == NumberSequenceKind.Warehouse && row.PartyId == 7);
    }

    private static (City City, Client Client, User User) SeedOrderParents(AppDbContext context)
    {
        var city = new City("الرياض");
        var client = new Client("عميل", "شارع الملك", city, "0501111111", "+966");
        var user = User.Create("tester", "hash", "مستخدم الاختبار");
        context.Cities.Add(city);
        context.Clients.Add(client);
        context.Users.Add(user);
        context.SaveChanges();
        return (city, client, user);
    }

    private static (Client Client, Supplier FirstSupplier, Supplier SecondSupplier) SeedSupplyParents(
        AppDbContext context)
    {
        var city = new City("الرياض");
        var client = new Client("عميل", "شارع الملك", city, "0501111111", "+966");
        var firstSupplier = new Supplier("مورد أول");
        var secondSupplier = new Supplier("مورد ثان");
        context.Cities.Add(city);
        context.Clients.Add(client);
        context.Suppliers.AddRange(firstSupplier, secondSupplier);
        context.SaveChanges();
        return (client, firstSupplier, secondSupplier);
    }

    private static (Client Client, Supplier Supplier) SeedSingleSupplierParents(AppDbContext context)
    {
        var city = new City("الرياض");
        var client = new Client("عميل", "شارع الملك", city, "0501111111", "+966");
        var supplier = new Supplier("مورد");
        context.Cities.Add(city);
        context.Clients.Add(client);
        context.Suppliers.Add(supplier);
        context.SaveChanges();
        return (client, supplier);
    }

    private static Deliveryman NewDeliveryman(AppDbContext context, City city, string name, string phone)
    {
        var deliveryman = new Deliveryman(name, city, phone);
        context.Deliverymen.Add(deliveryman);
        return deliveryman;
    }

    private static ShippingCarrier NewCarrier(AppDbContext context, City city, string name)
    {
        var carrier = new ShippingCarrier(
            name,
            25m,
            Phone.Create("0504444444", "+966").Value,
            Address.Create("طريق الملك فهد", city).Value);
        context.ShippingCarriers.Add(carrier);
        return carrier;
    }

    private static async Task<Supply> SaveSupplyAsync(AppDbContext context, UnitOfWork unitOfWork,
        Client client, Supplier supplier, DateTime createdAtUtc)
    {
        var supply = NewSupply(client, supplier, createdAtUtc);
        context.Supplies.Add(supply);
        await unitOfWork.SaveChangesAsync();
        return supply;
    }

    private static Supply NewSupply(Client client, Supplier supplier, DateTime createdAtUtc)
    {
        var supply = new Supply(new Order(client.Id, NewOrderNumber()), supplier.Id);
        supply.UpdateCreationDate(createdAtUtc);
        return supply;
    }

    private static Order NewOrder(Client client, DeliveryMethod method, int? deliverymanId = null, int? shippingCarrierId = null)
    {
        var order = new Order(client.Id, NewOrderNumber());
        order.UpdateHeader(client.Id, OrderSource.Online, method, deliverymanId, shippingCarrierId, null);
        return order;
    }

    private static string NewOrderNumber() => "ORD-" + Guid.NewGuid().ToString("N")[..8];

    private static OrderOperation Operation(User user) => new(user.Id, user.DisplayName, DateTime.UtcNow);

    private static UnitOfWork CreateUnitOfWork(AppDbContext context, DbContextOptions<AppDbContext> options) =>
        new(context, new NumberSequenceService(context, new AppDbContextFactory(options)));

    private sealed class FailHistoryInsertInterceptor : DbCommandInterceptor
    {
        public bool Enabled { get; set; }

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            ThrowIfHistoryInsert(command);
            return result;
        }

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result)
        {
            ThrowIfHistoryInsert(command);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            ThrowIfHistoryInsert(command);
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            ThrowIfHistoryInsert(command);
            return ValueTask.FromResult(result);
        }

        private void ThrowIfHistoryInsert(DbCommand command)
        {
            if (Enabled && command.CommandText.Contains("work_record_status_histories", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Injected status history database failure.");
        }
    }
}
