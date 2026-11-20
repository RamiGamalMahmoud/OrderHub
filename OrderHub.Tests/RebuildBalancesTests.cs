using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Rebuild.Contracts;
using OrderHub.Application.Features.Inventory.Rebuild.RebuildBalances;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OrderHub.Tests;

public sealed class RebuildBalancesTests
{
    [Fact]
    public async Task Rebuild_repairs_cached_balances_from_the_ledger_and_active_allocations()
    {
        using var db = new OrderAllocationTestDb();
        SeedMovement(db, db.MainWarehouse, db.Product, 10, InventoryDirection.In, InventoryMovementType.StockIn);
        SeedMovement(db, db.MainWarehouse, db.Product, 2, InventoryDirection.In, InventoryMovementType.ReturnDamagedIn);
        db.StockIn(db.MainWarehouse, db.Product, 7);
        db.SeedReservation(db.MainWarehouse, db.Product, 1);
        await SeedAllocationAsync(db, db.MainWarehouse, db.Product, 3);

        int movementsBefore = db.Context.InventoryMovements.Count();

        var result = await Handler(db, db.Sales.Id)
            .Handle(new RebuildBalancesCommand(InventoryRebuildScope.AllWarehouses), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.InspectedPairs);
        Assert.Equal(1, result.Value.ChangedPairs);

        StockBalance repaired = db.ReloadBalance(db.MainWarehouse, db.Product);
        Assert.Equal(10, repaired.StockOnHand);
        Assert.Equal(3, repaired.ReservedOnHand);
        Assert.Equal(2, repaired.DamagedQty);
        Assert.Equal(movementsBefore, db.Context.InventoryMovements.Count());

        RebuildLogDetails details = await db.Rebuilds.GetDetailsAsync(result.Value.LogId);
        Assert.NotNull(details);
        Assert.Equal(InventoryRebuildScope.AllWarehouses, details.Scope);
        Assert.Equal(db.Sales.Id, db.Context.InventoryRebuildLogs.Single().PerformedByUserId);

        RebuildLogLineRow line = Assert.Single(details.Lines);
        Assert.Equal(7, line.BeforeStockOnHand);
        Assert.Equal(1, line.BeforeReservedOnHand);
        Assert.Equal(0, line.BeforeDamagedQty);
        Assert.Equal(10, line.AfterStockOnHand);
        Assert.Equal(3, line.AfterReservedOnHand);
        Assert.Equal(2, line.AfterDamagedQty);
    }

    [Fact]
    public async Task Rebuild_is_idempotent_and_still_audits_the_second_run()
    {
        using var db = new OrderAllocationTestDb();
        SeedMovement(db, db.MainWarehouse, db.Product, 4, InventoryDirection.In, InventoryMovementType.OpeningStock);
        db.StockIn(db.MainWarehouse, db.Product, 1);

        var handler = Handler(db, db.Sales.Id);
        var first = await handler.Handle(new RebuildBalancesCommand(InventoryRebuildScope.AllWarehouses), default);
        Assert.True(first.IsSuccess);
        Assert.Equal(1, first.Value.ChangedPairs);

        StockBalance afterFirst = db.ReloadBalance(db.MainWarehouse, db.Product);
        Assert.Equal(4, afterFirst.StockOnHand);

        var second = await handler.Handle(new RebuildBalancesCommand(InventoryRebuildScope.AllWarehouses), default);
        Assert.True(second.IsSuccess);
        Assert.Equal(1, second.Value.InspectedPairs);
        Assert.Equal(0, second.Value.ChangedPairs);
        Assert.Empty(second.Value.Changes);

        StockBalance afterSecond = db.ReloadBalance(db.MainWarehouse, db.Product);
        Assert.Equal(4, afterSecond.StockOnHand);
        Assert.Equal(afterFirst.Version, afterSecond.Version);

        var history = await db.Rebuilds.GetHistoryAsync(10);
        Assert.Equal(2, history.Count);
        Assert.Equal(0, history[0].ChangedPairs);
        Assert.Equal(1, history[0].InspectedPairs);
    }

    [Fact]
    public async Task Single_warehouse_scope_repairs_only_the_selected_warehouse()
    {
        using var db = new OrderAllocationTestDb();
        SeedMovement(db, db.MainWarehouse, db.Product, 5, InventoryDirection.In, InventoryMovementType.StockIn);
        SeedMovement(db, db.SecondWarehouse, db.SecondProduct, 7, InventoryDirection.In, InventoryMovementType.StockIn);
        db.StockIn(db.MainWarehouse, db.Product, 1);
        db.StockIn(db.SecondWarehouse, db.SecondProduct, 2);

        var result = await Handler(db, db.Sales.Id).Handle(
            new RebuildBalancesCommand(InventoryRebuildScope.SingleWarehouse, db.MainWarehouse.Id),
            default);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.InspectedPairs);
        Assert.Equal(1, result.Value.ChangedPairs);

        Assert.Equal(5, db.ReloadBalance(db.MainWarehouse, db.Product).StockOnHand);
        Assert.Equal(2, db.ReloadBalance(db.SecondWarehouse, db.SecondProduct).StockOnHand);

        RebuildLogDetails details = await db.Rebuilds.GetDetailsAsync(result.Value.LogId);
        Assert.Equal(InventoryRebuildScope.SingleWarehouse, details.Scope);
        Assert.Equal(db.MainWarehouse.Id, db.Context.InventoryRebuildLogs.Single().WarehouseId);
        Assert.Equal(db.MainWarehouse.Id, Assert.Single(details.Lines).WarehouseId);
    }

    [Fact]
    public async Task Rebuild_creates_a_missing_balance_row_from_the_ledger()
    {
        using var db = new OrderAllocationTestDb();
        SeedMovement(db, db.MainWarehouse, db.SecondProduct, 6, InventoryDirection.In, InventoryMovementType.StockIn);

        var result = await Handler(db, db.Sales.Id)
            .Handle(new RebuildBalancesCommand(InventoryRebuildScope.AllWarehouses), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.ChangedPairs);

        StockBalance created = db.ReloadBalance(db.MainWarehouse, db.SecondProduct);
        Assert.NotNull(created);
        Assert.Equal(6, created.StockOnHand);
        Assert.Equal(0, created.ReservedOnHand);
        Assert.Equal(0, created.DamagedQty);

        RebuildLogLineRow line = Assert.Single((await db.Rebuilds.GetDetailsAsync(result.Value.LogId)).Lines);
        Assert.Equal(0, line.BeforeStockOnHand);
        Assert.Equal(6, line.AfterStockOnHand);
    }

    [Fact]
    public async Task Rebuild_nets_out_reversals_and_keeps_damaged_classification()
    {
        using var db = new OrderAllocationTestDb();
        InventoryMovement stockIn = SeedMovement(
            db, db.MainWarehouse, db.Product, 10, InventoryDirection.In, InventoryMovementType.StockIn);
        InventoryMovement damagedIn = SeedMovement(
            db, db.MainWarehouse, db.Product, 3, InventoryDirection.In, InventoryMovementType.ReturnDamagedIn);
        SeedMovement(
            db,
            db.MainWarehouse,
            db.Product,
            4,
            InventoryDirection.Out,
            InventoryMovementType.Reversal,
            stockIn.Id);
        SeedMovement(
            db,
            db.MainWarehouse,
            db.Product,
            1,
            InventoryDirection.Out,
            InventoryMovementType.Reversal,
            damagedIn.Id);

        var result = await Handler(db, db.Sales.Id)
            .Handle(new RebuildBalancesCommand(InventoryRebuildScope.AllWarehouses), default);

        Assert.True(result.IsSuccess);
        StockBalance created = db.ReloadBalance(db.MainWarehouse, db.Product);
        Assert.Equal(6, created.StockOnHand);
        Assert.Equal(2, created.DamagedQty);
    }

    [Fact]
    public async Task Rebuild_refuses_inconsistent_reserved_quantities_without_writing()
    {
        using var db = new OrderAllocationTestDb();
        SeedMovement(db, db.MainWarehouse, db.Product, 2, InventoryDirection.In, InventoryMovementType.StockIn);
        db.StockIn(db.MainWarehouse, db.Product, 2);
        await SeedAllocationAsync(db, db.MainWarehouse, db.Product, 5);

        var result = await Handler(db, db.Sales.Id)
            .Handle(new RebuildBalancesCommand(InventoryRebuildScope.AllWarehouses), default);

        Assert.False(result.IsSuccess);
        Assert.Equal("Conflict", result.ErrorCode);
        Assert.Contains("قفاز طبي", result.ErrorMessage);

        StockBalance untouched = db.ReloadBalance(db.MainWarehouse, db.Product);
        Assert.Equal(2, untouched.StockOnHand);
        Assert.Equal(0, untouched.ReservedOnHand);
        Assert.Equal(0, db.Context.InventoryRebuildLogs.Count());
    }

    [Fact]
    public async Task A_non_higher_manager_is_denied_by_the_authorization_pipeline()
    {
        var behavior = new AuthorizationBehavior<RebuildBalancesCommand, Result<RebuildBalancesResult>>(
            new HigherManagerGate(isHigherManager: false));

        await Assert.ThrowsAsync<AuthorizationDeniedException>(() =>
            behavior.Handle(
                new RebuildBalancesCommand(InventoryRebuildScope.AllWarehouses),
                _ => throw new InvalidOperationException("The handler must not run for a non-higher-manager."),
                CancellationToken.None));
    }

    private static RebuildBalancesCommandHandler Handler(OrderAllocationTestDb db, int? userId) =>
        new(db.Rebuilds, db.Warehouses, db.UnitOfWork, new TestCurrentUser(userId), db.Clock);

    private static InventoryMovement SeedMovement(
        OrderAllocationTestDb db,
        Warehouse warehouse,
        Product product,
        int quantity,
        InventoryDirection direction,
        InventoryMovementType type,
        int? reversalOfMovementId = null)
    {
        InventoryMovement movement = InventoryMovement.Create(
            warehouse.Id,
            product.Id,
            quantity,
            direction,
            type,
            db.Clock.GetUtcNow().UtcDateTime,
            db.Keeper.Id,
            "حركة اختبار",
            reversalOfMovementId: reversalOfMovementId);
        db.Context.InventoryMovements.Add(movement);
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        return movement;
    }

    private static async Task SeedAllocationAsync(
        OrderAllocationTestDb db,
        Warehouse warehouse,
        Product product,
        int quantity)
    {
        Order order = db.NewOrder($"ORD-RBLD-{Guid.NewGuid():N}"[..16], (product, quantity));
        Order loaded = await db.Orders.GetForUpdateAsync(order.Id);
        OrderItem item = loaded.OrderItems.Single();
        OrderInventoryAllocation allocation = OrderInventoryAllocation.Create(
            item.ProductId,
            AllocationSourceKind.Warehouse,
            quantity,
            warehouseId: warehouse.Id).Value;
        loaded.AddAllocation(allocation, item);
        await db.UnitOfWork.SaveChangesAsync(CancellationToken.None);
        db.Context.ChangeTracker.Clear();
    }

    private sealed class HigherManagerGate(bool isHigherManager) : IAuthorizationService
    {
        public Task<bool> IsAllowedAsync(
            PermissionFeature feature,
            PermissionLevel requiredLevel,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> IsHigherManagerAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(isHigherManager);
    }
}
