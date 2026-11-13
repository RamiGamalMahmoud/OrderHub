using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Inventory.Counts.ApproveCount;
using OrderHub.Application.Features.Inventory.Counts.CompleteCount;
using OrderHub.Application.Features.Inventory.Counts.GetCountDetails;
using OrderHub.Application.Features.Inventory.Counts.ReviewLateMovement;
using OrderHub.Application.Features.Inventory.Counts.SaveCountLine;
using OrderHub.Application.Features.Inventory.Counts.StartCount;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.Tests;

public sealed class InventoryCountApprovalTests
{
    private static readonly DateTime ReviewNow = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

    private static TimeProvider ReviewClock => new FixedClock(ReviewNow);

    [Fact]
    public async Task Approval_posts_both_adjustments_against_the_current_balance_once()
    {
        using var db = new OrderAllocationTestDb();
        InventoryCountTests.SeedStock(db, db.Product, sound: 10, damaged: 3);
        int countId = await PreparedCountAsync(db, countedQty: 7, countedDamagedQty: 5);
        int version = CountVersion(db, countId);

        var approved = await InventoryCountTests.ApproveHandler(db, db.Keeper.Id)
            .Handle(new ApproveCountCommand(countId, "تسوية الجرد", version), default);

        Assert.True(approved.IsSuccess);
        Assert.Equal(InventoryCountStatus.Approved, approved.Value.Status);

        db.Context.ChangeTracker.Clear();
        var balance = db.ReloadBalance(db.MainWarehouse, db.Product);
        Assert.Equal(7, balance.StockOnHand);
        Assert.Equal(5, balance.DamagedQty);

        var movements = db.Context.InventoryMovements.AsNoTracking()
            .Where(movement => movement.SourceCountId == countId)
            .OrderBy(movement => movement.Id)
            .ToList();
        Assert.Equal(2, movements.Count);
        Assert.Equal(InventoryMovementType.Adjustment, movements[0].MovementType);
        Assert.Equal(InventoryDirection.Out, movements[0].Direction);
        Assert.Equal(3, movements[0].Quantity);
        Assert.Equal(InventoryMovementType.DamagedAdjustment, movements[1].MovementType);
        Assert.Equal(InventoryDirection.In, movements[1].Direction);
        Assert.Equal(2, movements[1].Quantity);
        Assert.All(movements, movement => Assert.Equal("تسوية الجرد", movement.Note));

        Assert.False(db.Context.InventoryCountLines.AsNoTracking().Single().IsActive);

        var history = await InventoryCountTests.DetailsHandler(db, db.Keeper.Id, allowApprove: true)
            .Handle(new GetCountDetailsQuery(countId), default);
        Assert.True(history.IsSuccess);
        var archivedLine = Assert.Single(history.Value.Lines);
        Assert.Equal(7, archivedLine.CountedQty);
        Assert.Equal(5, archivedLine.CountedDamagedQty);

        var twice = await InventoryCountTests.ApproveHandler(db, db.Keeper.Id)
            .Handle(new ApproveCountCommand(countId, "إعادة", version), default);
        Assert.Equal("Conflict", twice.ErrorCode);

        db.Context.ChangeTracker.Clear();
        Assert.Equal(2, db.Context.InventoryMovements.AsNoTracking()
            .Count(movement => movement.SourceCountId == countId));
        Assert.Equal(7, db.ReloadBalance(db.MainWarehouse, db.Product).StockOnHand);
    }

    [Fact]
    public async Task Late_movement_pauses_approval_and_inclusion_recomputes_the_snapshot()
    {
        using var db = new OrderAllocationTestDb();
        InventoryCountTests.SeedStock(db, db.Product, sound: 10);
        int countId = await PreparedCountAsync(db, countedQty: 10, countedDamagedQty: 0);
        AddMovement(db, quantity: 2, InventoryDirection.In, InventoryMovementType.StockIn,
            new DateTime(2026, 11, 12, 8, 30, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 12, 9, 30, 0, DateTimeKind.Utc),
            damagedColumn: false);

        var details = await InventoryCountTests.DetailsHandler(db, db.Keeper.Id, allowApprove: true)
            .Handle(new GetCountDetailsQuery(countId), default);
        Assert.True(details.IsSuccess);
        Assert.True(details.Value.HasLateMovements);
        Assert.True(details.Value.Lines.Single().LateMovement);

        int version = CountVersion(db, countId);
        var paused = await InventoryCountTests.ApproveHandler(db, db.Sales.Id, ReviewClock)
            .Handle(new ApproveCountCommand(countId, null, version), default);
        Assert.Equal("ApprovalRequired", paused.ErrorCode);

        db.Context.ChangeTracker.Clear();
        Assert.Equal(12, db.ReloadBalance(db.MainWarehouse, db.Product).StockOnHand);
        Assert.Empty(db.Context.InventoryMovements.AsNoTracking()
            .Where(movement => movement.SourceCountId == countId).ToList());

        var reviewed = await ReviewHandler(db, db.Sales.Id, ReviewClock).Handle(
            new ReviewLateMovementCommand(countId, LineId(db, countId),
                LateMovementDecision.IncludedInCount, "شملها العدّ"),
            default);
        Assert.True(reviewed.IsSuccess);

        db.Context.ChangeTracker.Clear();
        InventoryCountLine line = db.Context.InventoryCountLines.AsNoTracking().Single();
        Assert.Equal(12, line.BookQty);
        Assert.Equal(-2, line.Delta);
        Assert.Equal(LateMovementDecision.IncludedInCount, line.ReviewDecision);
        Assert.Equal("شملها العدّ", line.ReviewNote);

        var approved = await InventoryCountTests.ApproveHandler(db, db.Sales.Id, ReviewClock)
            .Handle(new ApproveCountCommand(countId, null, reviewed.Value.Version), default);
        Assert.True(approved.IsSuccess);

        db.Context.ChangeTracker.Clear();
        Assert.Equal(10, db.ReloadBalance(db.MainWarehouse, db.Product).StockOnHand);
        var adjustment = Assert.Single(db.Context.InventoryMovements.AsNoTracking()
            .Where(movement => movement.SourceCountId == countId).ToList());
        Assert.Equal(InventoryMovementType.Adjustment, adjustment.MovementType);
        Assert.Equal(InventoryDirection.Out, adjustment.Direction);
        Assert.Equal(2, adjustment.Quantity);
    }

    [Fact]
    public async Task Recount_reopens_the_line_and_clears_the_counted_values()
    {
        using var db = new OrderAllocationTestDb();
        InventoryCountTests.SeedStock(db, db.Product, sound: 10);
        int countId = await PreparedCountAsync(db, countedQty: 10, countedDamagedQty: 0);
        AddMovement(db, quantity: 2, InventoryDirection.In, InventoryMovementType.StockIn,
            new DateTime(2026, 11, 12, 8, 30, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 12, 9, 30, 0, DateTimeKind.Utc),
            damagedColumn: false);

        int version = CountVersion(db, countId);
        Assert.Equal("ApprovalRequired", (await InventoryCountTests.ApproveHandler(db, db.Sales.Id, ReviewClock)
            .Handle(new ApproveCountCommand(countId, null, version), default)).ErrorCode);

        var recounted = await ReviewHandler(db, db.Sales.Id, ReviewClock).Handle(
            new ReviewLateMovementCommand(countId, LineId(db, countId),
                LateMovementDecision.Recounted, "أعيد العدّ"),
            default);
        Assert.True(recounted.IsSuccess);
        Assert.Equal(InventoryCountStatus.Open, recounted.Value.Status);

        db.Context.ChangeTracker.Clear();
        InventoryCountLine line = db.Context.InventoryCountLines.AsNoTracking().Single();
        Assert.Null(line.CountedQty);
        Assert.Null(line.CountedDamagedQty);
        Assert.Equal(12, line.BookQty);
        Assert.Equal(LateMovementDecision.Recounted, line.ReviewDecision);

        Assert.True((await InventoryCountTests.SaveHandler(db, db.Keeper.Id, ReviewClock)
            .Handle(new SaveCountLineCommand(countId, line.Id, 12, 0), default)).IsSuccess);
        var completed = await InventoryCountTests.CompleteHandler(db, db.Keeper.Id, ReviewClock)
            .Handle(new CompleteCountCommand(countId), default);
        Assert.True(completed.IsSuccess);

        var approved = await InventoryCountTests.ApproveHandler(db, db.Sales.Id, ReviewClock)
            .Handle(new ApproveCountCommand(countId, null, completed.Value.Version), default);
        Assert.True(approved.IsSuccess);
        Assert.Equal(InventoryCountStatus.Approved, approved.Value.Status);

        db.Context.ChangeTracker.Clear();
        Assert.Equal(12, db.ReloadBalance(db.MainWarehouse, db.Product).StockOnHand);
        Assert.Empty(db.Context.InventoryMovements.AsNoTracking()
            .Where(movement => movement.SourceCountId == countId).ToList());
    }

    [Fact]
    public async Task Approval_is_blocked_when_the_sound_result_drops_below_reservations()
    {
        using var db = new OrderAllocationTestDb();
        InventoryCountTests.SeedStock(db, db.Product, sound: 10);
        db.SeedReservation(db.MainWarehouse, db.Product, 8);
        int countId = await PreparedCountAsync(db, countedQty: 5, countedDamagedQty: 0);

        var blocked = await InventoryCountTests.ApproveHandler(db, db.Sales.Id)
            .Handle(new ApproveCountCommand(countId, null, CountVersion(db, countId)), default);

        Assert.Equal("Conflict", blocked.ErrorCode);
        Assert.Contains("المحجوزة", blocked.ErrorMessage);

        db.Context.ChangeTracker.Clear();
        Assert.Equal(10, db.ReloadBalance(db.MainWarehouse, db.Product).StockOnHand);
        Assert.Equal(InventoryCountStatus.UnderReview,
            db.Context.InventoryCounts.AsNoTracking().Single().Status);
        Assert.Empty(db.Context.InventoryMovements.AsNoTracking()
            .Where(movement => movement.SourceCountId == countId).ToList());
    }

    [Fact]
    public async Task Approval_is_blocked_when_the_damaged_result_goes_negative()
    {
        using var db = new OrderAllocationTestDb();
        InventoryCountTests.SeedStock(db, db.Product, sound: 5, damaged: 2);
        int countId = await PreparedCountAsync(db, countedQty: 5, countedDamagedQty: 0);
        AddMovement(db, quantity: 2, InventoryDirection.Out, InventoryMovementType.DamagedDisposal,
            new DateTime(2026, 11, 12, 9, 30, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 12, 9, 30, 0, DateTimeKind.Utc),
            damagedColumn: true);

        var blocked = await InventoryCountTests.ApproveHandler(db, db.Sales.Id)
            .Handle(new ApproveCountCommand(countId, null, CountVersion(db, countId)), default);

        Assert.Equal("Conflict", blocked.ErrorCode);
        Assert.Contains("سالبة", blocked.ErrorMessage);

        db.Context.ChangeTracker.Clear();
        Assert.Equal(0, db.ReloadBalance(db.MainWarehouse, db.Product).DamagedQty);
        Assert.Equal(5, db.ReloadBalance(db.MainWarehouse, db.Product).StockOnHand);
        Assert.Empty(db.Context.InventoryMovements.AsNoTracking()
            .Where(movement => movement.SourceCountId == countId).ToList());
    }

    [Fact]
    public async Task Stale_expected_version_is_rejected_without_side_effects()
    {
        using var db = new OrderAllocationTestDb();
        InventoryCountTests.SeedStock(db, db.Product, sound: 10);
        int countId = await PreparedCountAsync(db, countedQty: 8, countedDamagedQty: 0);
        int version = CountVersion(db, countId);

        var stale = await InventoryCountTests.ApproveHandler(db, db.Sales.Id)
            .Handle(new ApproveCountCommand(countId, null, version - 1), default);
        Assert.Equal("Conflict", stale.ErrorCode);

        db.Context.ChangeTracker.Clear();
        Assert.Equal(InventoryCountStatus.UnderReview,
            db.Context.InventoryCounts.AsNoTracking().Single().Status);
        Assert.Equal(10, db.ReloadBalance(db.MainWarehouse, db.Product).StockOnHand);

        var approved = await InventoryCountTests.ApproveHandler(db, db.Sales.Id)
            .Handle(new ApproveCountCommand(countId, null, version), default);
        Assert.True(approved.IsSuccess);
        Assert.Equal(8, db.ReloadBalance(db.MainWarehouse, db.Product).StockOnHand);
    }

    private static async Task<int> PreparedCountAsync(
        OrderAllocationTestDb db,
        int countedQty,
        int countedDamagedQty)
    {
        var start = await InventoryCountTests.StartHandler(db, db.Keeper.Id)
            .Handle(new StartCountCommand(InventoryCountScope.Full), default);
        Assert.True(start.IsSuccess);

        int lineId = LineId(db, start.Value.CountId);
        Assert.True((await InventoryCountTests.SaveHandler(db, db.Keeper.Id)
            .Handle(new SaveCountLineCommand(
                start.Value.CountId, lineId, countedQty, countedDamagedQty), default)).IsSuccess);

        var completed = await InventoryCountTests.CompleteHandler(db, db.Keeper.Id)
            .Handle(new CompleteCountCommand(start.Value.CountId), default);
        Assert.True(completed.IsSuccess);
        db.Context.ChangeTracker.Clear();
        return start.Value.CountId;
    }

    private static int LineId(OrderAllocationTestDb db, int countId) =>
        InventoryCountTests.LineId(db, countId);

    private static int CountVersion(OrderAllocationTestDb db, int countId)
    {
        db.Context.ChangeTracker.Clear();
        return db.Context.InventoryCounts.AsNoTracking()
            .Single(count => count.Id == countId).Version;
    }

    private static ReviewLateMovementCommandHandler ReviewHandler(
        OrderAllocationTestDb db,
        int userId,
        TimeProvider? clock = null) =>
        new(db.Counts, db.UnitOfWork, new TestCurrentUser(userId), clock ?? db.Clock);

    private static void AddMovement(
        OrderAllocationTestDb db,
        int quantity,
        InventoryDirection direction,
        InventoryMovementType movementType,
        DateTime effectiveAtUtc,
        DateTime createdAtUtc,
        bool damagedColumn)
    {
        db.Context.InventoryMovements.Add(InventoryMovement.Create(
            db.MainWarehouse.Id,
            db.Product.Id,
            quantity,
            direction,
            movementType,
            effectiveAtUtc,
            db.Keeper.Id,
            nowUtc: DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc)));

        StockBalance balance = db.Context.StockBalances.Single(
            row => row.WarehouseId == db.MainWarehouse.Id && row.ProductId == db.Product.Id);

        if (damagedColumn)
        {
            if (direction == InventoryDirection.In)
                balance.DamagedIn(quantity);
            else
                balance.DamagedDisposal(quantity);
        }
        else if (direction == InventoryDirection.In)
        {
            balance.ApplyIn(quantity);
        }
        else
        {
            balance.ApplyOut(quantity);
        }

        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }
}
