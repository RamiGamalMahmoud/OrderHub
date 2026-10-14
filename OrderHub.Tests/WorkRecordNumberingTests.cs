using System.Data.Common;
using Microsoft.Data.Sqlite;
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
        using var connection = OpenConnection();
        var options = Options(connection);
        using var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        var unitOfWork = CreateUnitOfWork(context, options);

        var jan31 = NewSupply(10, new DateTime(2026, 1, 31, 20, 59, 0, DateTimeKind.Utc));
        var sameMonth = NewSupply(10, new DateTime(2026, 1, 31, 20, 30, 0, DateTimeKind.Utc));
        var feb1 = NewSupply(10, new DateTime(2026, 1, 31, 21, 0, 0, DateTimeKind.Utc));
        var anotherSupplier = NewSupply(11, new DateTime(2026, 1, 31, 21, 0, 0, DateTimeKind.Utc));
        var beforeRiyadhYearBoundary = NewSupply(10, new DateTime(2026, 12, 31, 20, 59, 0, DateTimeKind.Utc));
        var atRiyadhYearBoundary = NewSupply(10, new DateTime(2026, 12, 31, 21, 0, 0, DateTimeKind.Utc));
        var nextYear = NewSupply(10, new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        context.Supplies.AddRange(jan31, sameMonth, feb1, anotherSupplier, beforeRiyadhYearBoundary, atRiyadhYearBoundary, nextYear);

        await unitOfWork.SaveChangesAsync();

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
        using var connection = OpenConnection();
        var options = Options(connection);
        using var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        var unitOfWork = CreateUnitOfWork(context, options);

        var tripOrder = NewOrder(DeliveryMethod.DeliveryMan, deliverymanId: 31);
        tripOrder.TransitionTo(OrderStatus.OutForDelivery, Operation(tripOrder));
        var shipmentOrder = NewOrder(DeliveryMethod.ShippingCompany, shippingCarrierId: 47);
        shipmentOrder.TransitionTo(OrderStatus.Shipped, Operation(shipmentOrder));
        var otherCarrierOrder = NewOrder(DeliveryMethod.ShippingCompany, shippingCarrierId: 48);
        otherCarrierOrder.TransitionTo(OrderStatus.Shipped, Operation(otherCarrierOrder));
        var secondTripOrder = NewOrder(DeliveryMethod.DeliveryMan, deliverymanId: 32);
        secondTripOrder.TransitionTo(OrderStatus.OutForDelivery, Operation(secondTripOrder));
        context.Orders.AddRange(tripOrder, shipmentOrder, otherCarrierOrder, secondTripOrder);

        await unitOfWork.SaveChangesAsync();

        var trip = Assert.Single(tripOrder.DeliveryTrips);
        var shipment = Assert.Single(shipmentOrder.Shipments);
        var otherCarrierShipment = Assert.Single(otherCarrierOrder.Shipments);
        var otherPartyTrip = Assert.Single(secondTripOrder.DeliveryTrips);
        Assert.Equal((31, 1L), (((IWorkRecord)trip).NumberingPartyId, trip.WorkRecordNumber));
        Assert.Equal((47, 1L), (((IWorkRecord)shipment).NumberingPartyId, shipment.WorkRecordNumber));
        Assert.Equal((48, 1L), (((IWorkRecord)otherCarrierShipment).NumberingPartyId, otherCarrierShipment.WorkRecordNumber));
        Assert.Equal((32, 1L), (((IWorkRecord)otherPartyTrip).NumberingPartyId, otherPartyTrip.WorkRecordNumber));
    }

    [Fact]
    public async Task Failed_attempt_keeps_its_number_and_retry_gets_a_new_number()
    {
        using var connection = OpenConnection();
        var options = Options(connection);
        using var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        var unitOfWork = CreateUnitOfWork(context, options);
        var order = NewOrder(DeliveryMethod.DeliveryMan, deliverymanId: 9);
        order.TransitionTo(OrderStatus.OutForDelivery, Operation(order));
        context.Orders.Add(order);
        await unitOfWork.SaveChangesAsync();
        var failedAttempt = Assert.Single(order.DeliveryTrips);
        var originalNumber = failedAttempt.WorkRecordNumber;

        order.TransitionTo(OrderStatus.DeliveryFailed, Operation(order), "تعذر التسليم");
        await unitOfWork.SaveChangesAsync();
        order.TransitionTo(OrderStatus.OutForDelivery, Operation(order));
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
        using var connection = OpenConnection();
        var options = Options(connection);
        using var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        var unitOfWork = CreateUnitOfWork(context, options);
        var supply = NewSupply(44, new DateTime(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc));
        context.Supplies.Add(supply);
        await unitOfWork.SaveChangesAsync();
        var number = supply.WorkRecordNumber;

        var cancelledSupply = NewSupply(44, new DateTime(2026, 10, 14, 12, 2, 0, DateTimeKind.Utc));
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
        using var connection = OpenConnection();
        var interceptor = new FailHistoryInsertInterceptor();
        var options = Options(connection, interceptor);
        using (var context = new AppDbContext(options))
        {
            context.Database.EnsureCreated();
            context.Supplies.Add(NewSupply(55, DateTime.UtcNow));
            interceptor.Enabled = true;
            await Assert.ThrowsAsync<PersistenceException>(() => CreateUnitOfWork(context, options).SaveChangesAsync());
        }

        interceptor.Enabled = false;
        using var verification = new AppDbContext(options);
        Assert.Empty(await verification.Supplies.ToListAsync());
        Assert.Empty(await verification.NumberSequences.ToListAsync());
    }

    [Fact]
    public async Task Concurrent_allocations_are_unique_for_one_party_and_month()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"orderhub-numbering-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={databasePath};Default Timeout=30")
                .UseSnakeCaseNamingConvention()
                .Options;
            using (var setup = new AppDbContext(options)) setup.Database.EnsureCreated();
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
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    private static Supply NewSupply(int supplierId, DateTime createdAtUtc)
    {
        var supply = new Supply(new Order(1, $"ORD-{Guid.NewGuid():N}"), supplierId);
        supply.UpdateCreationDate(createdAtUtc);
        return supply;
    }

    private static Order NewOrder(DeliveryMethod method, int? deliverymanId = null, int? shippingCarrierId = null)
    {
        var order = new Order(1, $"ORD-{Guid.NewGuid():N}");
        order.UpdateHeader(1, OrderSource.Online, method, deliverymanId, shippingCarrierId, null);
        return order;
    }

    private static OrderOperation Operation(Order order) => new(1, "Test", DateTime.UtcNow);

    private static UnitOfWork CreateUnitOfWork(AppDbContext context, DbContextOptions<AppDbContext> options) =>
        new(context, new NumberSequenceService(context, new AppDbContextFactory(options)));

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = OFF";
        command.ExecuteNonQuery();
        return connection;
    }

    private static DbContextOptions<AppDbContext> Options(SqliteConnection connection, DbCommandInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .UseSnakeCaseNamingConvention();
        if (interceptor is not null) builder.AddInterceptors(interceptor);
        return builder.Options;
    }

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
