using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Counts.ApproveCount;
using OrderHub.Application.Features.Inventory.Counts.CancelCount;
using OrderHub.Application.Features.Inventory.Counts.CompleteCount;
using OrderHub.Application.Features.Inventory.Counts.Contracts;
using OrderHub.Application.Features.Inventory.Counts.GetCountDetails;
using OrderHub.Application.Features.Inventory.Counts.GetCounts;
using OrderHub.Application.Features.Inventory.Counts.SaveCountLine;
using OrderHub.Application.Features.Inventory.Counts.StartCount;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.Tests;

public sealed class InventoryCountTests
{
    [Fact]
    public async Task Start_snapshots_book_columns_and_saving_never_touches_balances()
    {
        using var db = new OrderAllocationTestDb();
        SeedStock(db, db.Product, sound: 10, damaged: 3);

        var start = await StartHandler(db, db.Keeper.Id)
            .Handle(new StartCountCommand(InventoryCountScope.Full), default);

        Assert.True(start.IsSuccess);
        Assert.Equal(1, start.Value.LineCount);

        db.Context.ChangeTracker.Clear();
        InventoryCountLine line = db.Context.InventoryCountLines.AsNoTracking().Single();
        Assert.Equal(10, line.BookQty);
        Assert.Equal(3, line.BookDamagedQty);
        Assert.True(line.IsActive);

        Assert.True((await SaveHandler(db, db.Keeper.Id)
            .Handle(new SaveCountLineCommand(start.Value.CountId, line.Id, 8, 4), default)).IsSuccess);

        db.Context.ChangeTracker.Clear();
        Assert.Equal(0, db.Context.InventoryMovements.AsNoTracking()
            .Count(movement => movement.MovementType == InventoryMovementType.Adjustment ||
                movement.MovementType == InventoryMovementType.DamagedAdjustment));
        var balance = db.ReloadBalance(db.MainWarehouse, db.Product);
        Assert.Equal(10, balance.StockOnHand);
        Assert.Equal(3, balance.DamagedQty);

        InventoryCount count = db.Context.InventoryCounts.AsNoTracking().Single();
        Assert.Equal(InventoryCountStatus.Open, count.Status);
        Assert.Empty(db.Context.InventoryMovements.AsNoTracking()
            .Where(movement => movement.SourceCountId == count.Id).ToList());
    }

    [Fact]
    public async Task First_count_blocks_a_second_open_count_for_the_same_product()
    {
        using var db = new OrderAllocationTestDb();
        SeedStock(db, db.Product, sound: 5);

        Assert.True((await StartHandler(db, db.Keeper.Id)
            .Handle(new StartCountCommand(InventoryCountScope.Full), default)).IsSuccess);

        var second = await StartHandler(db, db.Keeper.Id)
            .Handle(new StartCountCommand(InventoryCountScope.Full), default);

        Assert.False(second.IsSuccess);
        Assert.Equal("Conflict", second.ErrorCode);
        Assert.Contains("جرد مفتوح", second.ErrorMessage);
        Assert.Equal(1, db.Context.InventoryCounts.Count());

        var products = await new GetCountsQueryHandler(
            db.Warehouses, db.Counts, AllowWrite(), new TestCurrentUser(db.Keeper.Id))
            .Handle(new GetCountsQuery(), default);
        Assert.True(products.IsSuccess);
        Assert.True(products.Value.Products.Single().HasOpenCount);
    }

    [Fact]
    public async Task Complete_requires_both_counted_columns_and_keeper_binding()
    {
        using var db = new OrderAllocationTestDb();
        SeedStock(db, db.Product, sound: 4);
        var start = await StartHandler(db, db.Keeper.Id)
            .Handle(new StartCountCommand(InventoryCountScope.Full), default);
        int lineId = LineId(db, start.Value.CountId);

        var foreign = await SaveHandler(db, db.Sales.Id)
            .Handle(new SaveCountLineCommand(start.Value.CountId, lineId, 4, 0), default);
        Assert.Equal("Forbidden", foreign.ErrorCode);

        Assert.True((await SaveHandler(db, db.Keeper.Id)
            .Handle(new SaveCountLineCommand(start.Value.CountId, lineId, 4, null), default)).IsSuccess);

        var incomplete = await CompleteHandler(db, db.Keeper.Id)
            .Handle(new CompleteCountCommand(start.Value.CountId), default);
        Assert.Equal("Conflict", incomplete.ErrorCode);

        Assert.True((await SaveHandler(db, db.Keeper.Id)
            .Handle(new SaveCountLineCommand(start.Value.CountId, lineId, 4, 0), default)).IsSuccess);

        var completed = await CompleteHandler(db, db.Keeper.Id)
            .Handle(new CompleteCountCommand(start.Value.CountId), default);
        Assert.True(completed.IsSuccess);
        Assert.Equal(InventoryCountStatus.UnderReview, completed.Value.Status);
    }

    [Fact]
    public async Task Products_scope_only_counts_the_selected_products_and_can_run_per_product()
    {
        using var db = new OrderAllocationTestDb();
        SeedStock(db, db.Product, sound: 4);
        SeedStock(db, db.SecondProduct, sound: 6);

        var first = await StartHandler(db, db.Keeper.Id).Handle(
            new StartCountCommand(InventoryCountScope.Products, [db.Product.Id]),
            default);
        Assert.True(first.IsSuccess);
        Assert.Equal(1, first.Value.LineCount);

        var full = await StartHandler(db, db.Keeper.Id)
            .Handle(new StartCountCommand(InventoryCountScope.Full), default);
        Assert.Equal("Conflict", full.ErrorCode);

        var second = await StartHandler(db, db.Keeper.Id).Handle(
            new StartCountCommand(InventoryCountScope.Products, [db.SecondProduct.Id]),
            default);
        Assert.True(second.IsSuccess);
        Assert.Equal(2, db.Context.InventoryCounts.Count());

        var duplicate = await StartHandler(db, db.Keeper.Id).Handle(
            new StartCountCommand(InventoryCountScope.Products, [db.Product.Id]),
            default);
        Assert.Equal("Conflict", duplicate.ErrorCode);
    }

    [Fact]
    public async Task Cancel_requires_a_reason_keeps_balances_and_releases_the_products()
    {
        using var db = new OrderAllocationTestDb();
        SeedStock(db, db.Product, sound: 5);
        var start = await StartHandler(db, db.Keeper.Id)
            .Handle(new StartCountCommand(InventoryCountScope.Full), default);

        var handled = await CancelHandler(db, db.Keeper.Id)
            .Handle(new CancelCountCommand(start.Value.CountId, string.Empty), default);
        Assert.Equal("سبب الإلغاء مطلوب.", handled.ErrorMessage);

        var cancelled = await CancelHandler(db, db.Keeper.Id)
            .Handle(new CancelCountCommand(start.Value.CountId, "عدّ خاطئ"), default);
        Assert.True(cancelled.IsSuccess);
        Assert.Equal(InventoryCountStatus.Cancelled, cancelled.Value.Status);

        db.Context.ChangeTracker.Clear();
        Assert.False(db.Context.InventoryCountLines.AsNoTracking().Single().IsActive);
        Assert.Equal(5, db.ReloadBalance(db.MainWarehouse, db.Product).StockOnHand);
        Assert.Empty(db.Context.InventoryMovements.AsNoTracking()
            .Where(movement => movement.SourceCountId == start.Value.CountId).ToList());

        Assert.True((await StartHandler(db, db.Keeper.Id)
            .Handle(new StartCountCommand(InventoryCountScope.Full), default)).IsSuccess);
    }

    [Fact]
    public async Task Open_counts_follow_the_current_keeper_after_a_transfer()
    {
        using var db = new OrderAllocationTestDb();
        SeedStock(db, db.Product, sound: 5);
        var start = await StartHandler(db, db.Keeper.Id)
            .Handle(new StartCountCommand(InventoryCountScope.Full), default);
        int lineId = LineId(db, start.Value.CountId);

        db.Context.Warehouses.Single(warehouse => warehouse.Id == db.MainWarehouse.Id)
            .AssignKeeper(db.Sales.Id);
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var oldKeeper = await SaveHandler(db, db.Keeper.Id)
            .Handle(new SaveCountLineCommand(start.Value.CountId, lineId, 5, 0), default);
        Assert.Equal("Forbidden", oldKeeper.ErrorCode);

        var newKeeper = await SaveHandler(db, db.Sales.Id)
            .Handle(new SaveCountLineCommand(start.Value.CountId, lineId, 5, 0), default);
        Assert.True(newKeeper.IsSuccess);

        var completed = await CompleteHandler(db, db.Sales.Id)
            .Handle(new CompleteCountCommand(start.Value.CountId), default);
        Assert.True(completed.IsSuccess);
    }

    [Fact]
    public async Task Overview_and_details_respect_keeper_and_manager_scope()
    {
        using var db = new OrderAllocationTestDb();
        SeedStock(db, db.Product, sound: 6, damaged: 1);
        var start = await StartHandler(db, db.Keeper.Id)
            .Handle(new StartCountCommand(InventoryCountScope.Full), default);
        int lineId = LineId(db, start.Value.CountId);
        Assert.True((await SaveHandler(db, db.Keeper.Id)
            .Handle(new SaveCountLineCommand(start.Value.CountId, lineId, 5, 2), default)).IsSuccess);

        var keeperRows = await new GetCountsQueryHandler(
            db.Warehouses, db.Counts, AllowWrite(), new TestCurrentUser(db.Keeper.Id))
            .Handle(new GetCountsQuery(), default);
        Assert.True(keeperRows.IsSuccess);
        Assert.True(keeperRows.Value.IsKeeper);
        Assert.Equal(db.MainWarehouse.Name.Value, keeperRows.Value.WarehouseName);
        Assert.Single(keeperRows.Value.Counts);
        Assert.Equal(1, keeperRows.Value.Counts[0].CountedLineCount);

        var managerRows = await new GetCountsQueryHandler(
            db.Warehouses, db.Counts, AllowApprove(), new TestCurrentUser(db.Sales.Id))
            .Handle(new GetCountsQuery(), default);
        Assert.True(managerRows.IsSuccess);
        Assert.False(managerRows.Value.IsKeeper);
        Assert.Equal("كل المخازن", managerRows.Value.WarehouseName);
        Assert.Single(managerRows.Value.Counts);

        var keeperDetails = await DetailsHandler(db, db.Keeper.Id)
            .Handle(new GetCountDetailsQuery(start.Value.CountId), default);
        Assert.True(keeperDetails.IsSuccess);
        Assert.True(keeperDetails.Value.IsKeeper);
        Assert.True(keeperDetails.Value.AllCounted);

        var managerDetails = await DetailsHandler(db, db.Sales.Id, allowApprove: true)
            .Handle(new GetCountDetailsQuery(start.Value.CountId), default);
        Assert.True(managerDetails.IsSuccess);
        Assert.False(managerDetails.Value.IsKeeper);

        var outsider = await DetailsHandler(db, db.Sales.Id)
            .Handle(new GetCountDetailsQuery(start.Value.CountId), default);
        Assert.Equal("Forbidden", outsider.ErrorCode);
    }

    internal static void SeedStock(
        OrderAllocationTestDb db,
        Product product,
        int sound,
        int damaged = 0)
    {
        var balance = StockBalance.Create(db.MainWarehouse.Id, product.Id);
        if (sound > 0)
            balance.ApplyIn(sound);
        if (damaged > 0)
            balance.DamagedIn(damaged);
        db.Context.StockBalances.Add(balance);
        db.Context.SaveChanges();

        DateTime stamp = new(2026, 11, 12, 8, 0, 0, DateTimeKind.Utc);
        if (sound > 0)
        {
            db.Context.InventoryMovements.Add(InventoryMovement.Create(
                db.MainWarehouse.Id,
                product.Id,
                sound,
                InventoryDirection.In,
                InventoryMovementType.StockIn,
                stamp,
                db.Keeper.Id,
                nowUtc: stamp));
        }

        if (damaged > 0)
        {
            db.Context.InventoryMovements.Add(InventoryMovement.Create(
                db.MainWarehouse.Id,
                product.Id,
                damaged,
                InventoryDirection.In,
                InventoryMovementType.ReturnDamagedIn,
                stamp,
                db.Keeper.Id,
                nowUtc: stamp));
        }

        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    internal static int LineId(OrderAllocationTestDb db, int countId)
    {
        db.Context.ChangeTracker.Clear();
        return db.Context.InventoryCountLines.AsNoTracking()
            .Single(line => line.CountId == countId).Id;
    }

    internal static StartCountCommandHandler StartHandler(
        OrderAllocationTestDb db,
        int userId,
        TimeProvider? clock = null) =>
        new(db.Warehouses, db.Counts, db.UnitOfWork, new TestCurrentUser(userId), clock ?? db.Clock);

    internal static SaveCountLineCommandHandler SaveHandler(
        OrderAllocationTestDb db,
        int userId,
        TimeProvider? clock = null) =>
        new(db.Counts, db.UnitOfWork, new TestCurrentUser(userId), clock ?? db.Clock);

    internal static CompleteCountCommandHandler CompleteHandler(
        OrderAllocationTestDb db,
        int userId,
        TimeProvider? clock = null) =>
        new(db.Counts, db.UnitOfWork, new TestCurrentUser(userId), clock ?? db.Clock);

    internal static ApproveCountCommandHandler ApproveHandler(
        OrderAllocationTestDb db,
        int userId,
        TimeProvider? clock = null) =>
        new(db.Counts, db.Inventory, db.UnitOfWork, new TestCurrentUser(userId), clock ?? db.Clock);

    internal static CancelCountCommandHandler CancelHandler(OrderAllocationTestDb db, int userId) =>
        new(db.Counts, AllowApprove(), db.UnitOfWork, new TestCurrentUser(userId), db.Clock);

    internal static GetCountDetailsQueryHandler DetailsHandler(
        OrderAllocationTestDb db,
        int userId,
        bool allowApprove = false) =>
        new(db.Warehouses, db.Counts, allowApprove ? AllowApprove() : AllowWrite(), new TestCurrentUser(userId));

    internal static IAuthorizationService AllowApprove() =>
        new StubPermissions((_, level) => level is PermissionLevel.Read or PermissionLevel.Write or PermissionLevel.Approve);

    internal static IAuthorizationService AllowWrite() =>
        new StubPermissions((_, level) => level is PermissionLevel.Read or PermissionLevel.Write);
}
