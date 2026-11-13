using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Inventory.Contracts;
using OrderHub.Application.Features.Inventory.GetStockInContext;
using OrderHub.Application.Features.Inventory.RecordStockIn;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OrderHub.Tests;

public sealed class StockInHandlerTests
{
    private static readonly DateTime Now = new(2026, 11, 12, 9, 0, 0, DateTimeKind.Utc);

    private static RecordStockInCommandHandler Handler(
        StockInTestDb db,
        User user,
        IInventoryRepository inventory = null,
        TimeProvider clock = null) =>
        new(
            db.Warehouses,
            inventory ?? db.Inventory,
            db.Receipts,
            db.Products,
            db.AuthorizationFor(user),
            new FixedCurrentUser(user),
            db.UnitOfWork,
            clock ?? new FixedClock(Now));

    private static StockInInput Input(
        int warehouseId,
        params StockInLineInput[] lines) =>
        new(warehouseId, StockReceiptType.StockIn, null, null, lines, []);

    [Fact]
    public async Task RecordStockIn_PersistsReceiptLedgerAndBalance()
    {
        using var db = new StockInTestDb();

        var result = await Handler(db, db.Keeper).Handle(
            new RecordStockInCommand(Input(
                db.MainWarehouse.Id,
                new StockInLineInput(db.LinkedProduct.Id, 7),
                new StockInLineInput(db.SecondLinkedProduct.Id, 3))),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(2, result.Value.Lines.Count);

        db.Context.ChangeTracker.Clear();

        StockReceipt receipt = db.Context.StockReceipts
            .Include(entity => entity.Lines)
            .Single();
        Assert.Equal(2, receipt.Lines.Count);
        Assert.Equal(Now, receipt.CreatedAt);
        Assert.Equal(db.Keeper.Id, receipt.CreatedByUserId);
        Assert.Equal(StockReceiptType.StockIn, receipt.ReceiptType);

        var movements = db.Context.InventoryMovements
            .OrderBy(movement => movement.ProductId)
            .ToList();
        Assert.Equal(2, movements.Count);
        Assert.All(movements, movement =>
        {
            Assert.Equal(InventoryDirection.In, movement.Direction);
            Assert.Equal(InventoryMovementType.StockIn, movement.MovementType);
            Assert.Equal(receipt.Id, movement.SourceStockReceiptId);
            Assert.Equal(Now, movement.EffectiveAtUtc);
            Assert.Equal(db.Keeper.Id, movement.CreatedByUserId);
        });

        Assert.Equal(7, movements.Single(m => m.ProductId == db.LinkedProduct.Id).Quantity);
        Assert.Equal(3, movements.Single(m => m.ProductId == db.SecondLinkedProduct.Id).Quantity);

        var balances = db.Context.StockBalances
            .OrderBy(balance => balance.ProductId)
            .ToList();
        Assert.Equal(2, balances.Count);
        Assert.Equal(7, balances.Single(b => b.ProductId == db.LinkedProduct.Id).StockOnHand);
        Assert.Equal(3, balances.Single(b => b.ProductId == db.SecondLinkedProduct.Id).StockOnHand);
    }

    [Fact]
    public async Task RecordStockIn_AggregatesDuplicateLines()
    {
        using var db = new StockInTestDb();

        var result = await Handler(db, db.Keeper).Handle(
            new RecordStockInCommand(Input(
                db.MainWarehouse.Id,
                new StockInLineInput(db.LinkedProduct.Id, 2),
                new StockInLineInput(db.LinkedProduct.Id, 3))),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.ErrorMessage);

        db.Context.ChangeTracker.Clear();

        StockReceipt receipt = db.Context.StockReceipts
            .Include(entity => entity.Lines)
            .Single();
        StockReceiptLine line = Assert.Single(receipt.Lines);
        Assert.Equal(5, line.Quantity);
        Assert.Equal(5, db.Context.StockBalances.Single().StockOnHand);
        Assert.Equal(5, db.Context.InventoryMovements.Single().Quantity);
    }

    [Fact]
    public async Task RecordStockIn_RollsBackWhenLedgerWriteFails()
    {
        using var db = new StockInTestDb();
        var inventory = new FailingInventoryRepository(db.Inventory);

        var result = await Handler(db, db.Keeper, inventory).Handle(
            new RecordStockInCommand(Input(db.MainWarehouse.Id, new StockInLineInput(db.LinkedProduct.Id, 4))),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Persistence", result.ErrorCode);

        db.Context.ChangeTracker.Clear();
        Assert.Empty(db.Context.StockReceipts);
        Assert.Empty(db.Context.StockReceiptLines);
        Assert.Empty(db.Context.InventoryMovements);
        Assert.Empty(db.Context.StockBalances);
    }

    [Fact]
    public async Task RecordStockIn_RequiresProductsRead()
    {
        using var db = new StockInTestDb();

        var result = await Handler(db, db.KeeperWithoutProductsRead).Handle(
            new RecordStockInCommand(Input(db.MainWarehouse.Id, new StockInLineInput(db.LinkedProduct.Id, 1))),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Forbidden", result.ErrorCode);
        Assert.Contains("قراءة المنتجات", result.ErrorMessage);
    }

    [Fact]
    public async Task RecordStockIn_RejectsNonKeeper()
    {
        using var db = new StockInTestDb();

        var result = await Handler(db, db.OtherKeeper).Handle(
            new RecordStockInCommand(Input(db.MainWarehouse.Id, new StockInLineInput(db.LinkedProduct.Id, 1))),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Forbidden", result.ErrorCode);
        Assert.Contains("أمين المخزن", result.ErrorMessage);

        db.Context.ChangeTracker.Clear();
        Assert.Empty(db.Context.StockReceipts);
    }

    [Fact]
    public async Task RecordStockIn_BlocksOpeningBalanceAfterFirstMovement()
    {
        using var db = new StockInTestDb();

        var first = await Handler(db, db.Keeper).Handle(
            new RecordStockInCommand(Input(db.MainWarehouse.Id, new StockInLineInput(db.LinkedProduct.Id, 1))),
            CancellationToken.None);
        Assert.True(first.IsSuccess, first.ErrorMessage);

        var opening = await Handler(db, db.Keeper).Handle(
            new RecordStockInCommand(new StockInInput(
                db.MainWarehouse.Id,
                StockReceiptType.Opening,
                null,
                null,
                [new StockInLineInput(db.SecondLinkedProduct.Id, 9)],
                [])),
            CancellationToken.None);

        Assert.False(opening.IsSuccess);
        Assert.Contains("الرصيد الافتتاحي", opening.ErrorMessage);
    }

    [Fact]
    public async Task RecordStockIn_AllowsOpeningWhenWarehouseHasNoMovements()
    {
        using var db = new StockInTestDb();

        var result = await Handler(db, db.Keeper).Handle(
            new RecordStockInCommand(new StockInInput(
                db.MainWarehouse.Id,
                StockReceiptType.Opening,
                null,
                "  رصيد بداية  ",
                [new StockInLineInput(db.LinkedProduct.Id, 12)],
                [])),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.ErrorMessage);

        db.Context.ChangeTracker.Clear();
        Assert.Equal(12, db.Context.StockBalances.Single().StockOnHand);
        Assert.Equal(
            InventoryMovementType.OpeningStock,
            db.Context.InventoryMovements.Single().MovementType);
        Assert.Equal("رصيد بداية", db.Context.StockReceipts.Single().ReferenceNote);
    }

    [Fact]
    public async Task RecordStockIn_RejectsSupplierNotLinkedToEveryProduct()
    {
        using var db = new StockInTestDb();

        var result = await Handler(db, db.Keeper).Handle(
            new RecordStockInCommand(new StockInInput(
                db.MainWarehouse.Id,
                StockReceiptType.StockIn,
                db.Supplier.Id,
                null,
                [new StockInLineInput(db.LinkedProduct.Id, 1), new StockInLineInput(db.UnlinkedProduct.Id, 1)],
                [])),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("غير مرتبط بالمورد", result.ErrorMessage);

        db.Context.ChangeTracker.Clear();
        Assert.Empty(db.Context.StockReceipts);
    }

    [Fact]
    public async Task RecordStockIn_AcceptsLinkedSupplierAndPersistsIt()
    {
        using var db = new StockInTestDb();

        var result = await Handler(db, db.Keeper).Handle(
            new RecordStockInCommand(new StockInInput(
                db.MainWarehouse.Id,
                StockReceiptType.StockIn,
                db.Supplier.Id,
                null,
                [new StockInLineInput(db.LinkedProduct.Id, 1), new StockInLineInput(db.SecondLinkedProduct.Id, 2)],
                [])),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(db.Supplier.Id, result.Value.SupplierId);

        db.Context.ChangeTracker.Clear();
        Assert.Equal(db.Supplier.Id, db.Context.StockReceipts.Single().SupplierId);
    }

    [Fact]
    public async Task RecordStockIn_AttachesDraftAttachments()
    {
        using var db = new StockInTestDb();
        StockReceiptAttachment draft = db.SeedDraft(db.Keeper, db.MainWarehouse);

        var result = await Handler(db, db.Keeper).Handle(
            new RecordStockInCommand(new StockInInput(
                db.MainWarehouse.Id,
                StockReceiptType.StockIn,
                null,
                null,
                [new StockInLineInput(db.LinkedProduct.Id, 1)],
                [draft.Id])),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        StockAttachmentDetails attachment = Assert.Single(result.Value.Attachments);
        Assert.Equal(draft.Id, attachment.Id);

        db.Context.ChangeTracker.Clear();
        StockReceipt saved = db.Context.StockReceipts.Single();
        StockReceiptAttachment stored = db.Context.StockReceiptAttachments.Single();
        Assert.Equal(saved.Id, stored.StockReceiptId);
        Assert.False(stored.IsDraft);
    }

    [Fact]
    public async Task RecordStockIn_RejectsAttachmentUploadedByAnotherUser()
    {
        using var db = new StockInTestDb();
        StockReceiptAttachment foreignDraft = db.SeedDraft(db.OtherKeeper, db.MainWarehouse);

        var result = await Handler(db, db.Keeper).Handle(
            new RecordStockInCommand(new StockInInput(
                db.MainWarehouse.Id,
                StockReceiptType.StockIn,
                null,
                null,
                [new StockInLineInput(db.LinkedProduct.Id, 1)],
                [foreignDraft.Id])),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("أحد المرفقات غير متاح", result.ErrorMessage);

        db.Context.ChangeTracker.Clear();
        Assert.Empty(db.Context.StockReceipts);
    }

    [Fact]
    public async Task GetStockInContext_ReturnsKeeperWarehouseAndOpeningState()
    {
        using var db = new StockInTestDb();
        var handler = new GetStockInContextQueryHandler(
            db.Warehouses,
            db.Inventory,
            db.AuthorizationFor(db.Keeper),
            new FixedCurrentUser(db.Keeper));

        var context = await handler.Handle(new GetStockInContextQuery(), CancellationToken.None);

        Assert.True(context.IsSuccess, context.ErrorMessage);
        Assert.Equal(db.MainWarehouse.Id, context.Value.WarehouseId);
        Assert.True(context.Value.IsOpeningAllowed);
        Assert.True(context.Value.CanReadProducts);
        Assert.True(context.Value.CanSelectSupplier);

        await Handler(db, db.Keeper).Handle(
            new RecordStockInCommand(Input(db.MainWarehouse.Id, new StockInLineInput(db.LinkedProduct.Id, 1))),
            CancellationToken.None);

        var afterMovement = await handler.Handle(new GetStockInContextQuery(), CancellationToken.None);

        Assert.True(afterMovement.IsSuccess, afterMovement.ErrorMessage);
        Assert.False(afterMovement.Value.IsOpeningAllowed);
    }

    [Fact]
    public async Task GetStockInContext_RejectsUserWithoutWarehouseBinding()
    {
        using var db = new StockInTestDb();
        var handler = new GetStockInContextQueryHandler(
            db.Warehouses,
            db.Inventory,
            db.AuthorizationFor(db.Manager),
            new FixedCurrentUser(db.Manager));

        var result = await handler.Handle(new GetStockInContextQuery(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Forbidden", result.ErrorCode);
    }

    /// <summary>Delegates every read to the real repository but fails when the ledger row is added.</summary>
    private sealed class FailingInventoryRepository(IInventoryRepository inner) : IInventoryRepository
    {
        public Task<System.Collections.Generic.IReadOnlyList<OrderHub.Application.Features.Inventory.GetStockAvailability.StockAvailabilityRow>> GetAvailabilityAsync(int productId, CancellationToken cancellationToken = default) =>
            inner.GetAvailabilityAsync(productId, cancellationToken);

        public Task LockBalancesAsync(System.Collections.Generic.IReadOnlyCollection<(int WarehouseId, int ProductId)> keys, CancellationToken cancellationToken = default) =>
            inner.LockBalancesAsync(keys, cancellationToken);

        public Task<StockBalance> GetStockItemAsync(int warehouseId, int productId, CancellationToken cancellationToken = default) =>
            inner.GetStockItemAsync(warehouseId, productId, cancellationToken);

        public Task<System.Collections.Generic.IReadOnlyList<StockBalance>> GetStockAsync(int warehouseId, CancellationToken cancellationToken = default) =>
            inner.GetStockAsync(warehouseId, cancellationToken);

        public Task<StockBalance> GetOrCreateStockItemAsync(int warehouseId, int productId, CancellationToken cancellationToken = default) =>
            inner.GetOrCreateStockItemAsync(warehouseId, productId, cancellationToken);

        public void Add(InventoryMovement movement) =>
            throw new PersistenceException("ledger write failed", new InvalidOperationException("boom"));

        public Task<bool> TryReserveAsync(int warehouseId, int productId, int quantity, CancellationToken cancellationToken = default) =>
            inner.TryReserveAsync(warehouseId, productId, quantity, cancellationToken);

        public Task<bool> ReleaseAsync(int warehouseId, int productId, int quantity, CancellationToken cancellationToken = default) =>
            inner.ReleaseAsync(warehouseId, productId, quantity, cancellationToken);

        public Task<bool> ConsumeAsync(int warehouseId, int productId, int quantity, CancellationToken cancellationToken = default) =>
            inner.ConsumeAsync(warehouseId, productId, quantity, cancellationToken);

        public Task<System.Collections.Generic.IReadOnlyList<InventoryMovement>> GetLedgerAsync(int warehouseId, int? productId, int skip, int take, CancellationToken cancellationToken = default) =>
            inner.GetLedgerAsync(warehouseId, productId, skip, take, cancellationToken);

        public Task<int> CountLedgerAsync(int warehouseId, int? productId, CancellationToken cancellationToken = default) =>
            inner.CountLedgerAsync(warehouseId, productId, cancellationToken);

        public Task<bool> HasMovementsAsync(int warehouseId, CancellationToken cancellationToken = default) =>
            inner.HasMovementsAsync(warehouseId, cancellationToken);

        public Task<System.Collections.Generic.IReadOnlyList<OrderHub.Application.Features.Inventory.WarehouseLedger.WarehouseLedgerProjectionRow>> GetWarehouseLedgerPageAsync(int warehouseId, int productId, int skip, int take, CancellationToken cancellationToken = default) =>
            inner.GetWarehouseLedgerPageAsync(warehouseId, productId, skip, take, cancellationToken);

        public Task<OrderHub.Application.Features.Inventory.WarehouseLedger.WarehouseProductStockRow> GetWarehouseProductStockAsync(int warehouseId, int productId, CancellationToken cancellationToken = default) =>
            inner.GetWarehouseProductStockAsync(warehouseId, productId, cancellationToken);

        public Task<OrderHub.Application.Features.Inventory.WarehouseStock.WarehouseStockTotalsRow> GetWarehouseStockTotalsAsync(int warehouseId, CancellationToken cancellationToken = default) =>
            inner.GetWarehouseStockTotalsAsync(warehouseId, cancellationToken);

        public Task<OrderHub.Application.Features.Inventory.WarehouseStock.WarehouseStockTotalsRow> GetWarehouseStockFilteredTotalsAsync(OrderHub.Application.Features.Inventory.WarehouseStock.WarehouseStockQueryFilter filter, CancellationToken cancellationToken = default) =>
            inner.GetWarehouseStockFilteredTotalsAsync(filter, cancellationToken);

        public Task<int> CountWarehouseStockAsync(OrderHub.Application.Features.Inventory.WarehouseStock.WarehouseStockQueryFilter filter, CancellationToken cancellationToken = default) =>
            inner.CountWarehouseStockAsync(filter, cancellationToken);

        public Task<int> CountWarehouseStockWithStockAsync(OrderHub.Application.Features.Inventory.WarehouseStock.WarehouseStockQueryFilter filter, CancellationToken cancellationToken = default) =>
            inner.CountWarehouseStockWithStockAsync(filter, cancellationToken);

        public Task<System.Collections.Generic.IReadOnlyList<OrderHub.Application.Features.Inventory.WarehouseStock.WarehouseStockProjectionRow>> GetWarehouseStockPageAsync(OrderHub.Application.Features.Inventory.WarehouseStock.WarehouseStockQueryFilter filter, CancellationToken cancellationToken = default) =>
            inner.GetWarehouseStockPageAsync(filter, cancellationToken);

        public Task<System.Collections.Generic.IReadOnlyList<OrderHub.Application.Features.Inventory.WarehouseStock.StockCategoryOption>> GetWarehouseStockCategoriesAsync(int warehouseId, CancellationToken cancellationToken = default) =>
            inner.GetWarehouseStockCategoriesAsync(warehouseId, cancellationToken);

        public Task<OrderHub.Application.Features.Inventory.WarehouseStock.WarehouseStockTotalsRow> GetConsolidatedStockTotalsAsync(System.Collections.Generic.IReadOnlyCollection<int> warehouseIds, CancellationToken cancellationToken = default) =>
            inner.GetConsolidatedStockTotalsAsync(warehouseIds, cancellationToken);

        public Task<OrderHub.Application.Features.Inventory.WarehouseStock.WarehouseStockTotalsRow> GetConsolidatedStockFilteredTotalsAsync(OrderHub.Application.Features.Inventory.ConsolidatedBalances.ConsolidatedStockQueryFilter filter, CancellationToken cancellationToken = default) =>
            inner.GetConsolidatedStockFilteredTotalsAsync(filter, cancellationToken);

        public Task<int> CountConsolidatedStockAsync(OrderHub.Application.Features.Inventory.ConsolidatedBalances.ConsolidatedStockQueryFilter filter, CancellationToken cancellationToken = default) =>
            inner.CountConsolidatedStockAsync(filter, cancellationToken);

        public Task<int> CountConsolidatedStockWithStockAsync(OrderHub.Application.Features.Inventory.ConsolidatedBalances.ConsolidatedStockQueryFilter filter, CancellationToken cancellationToken = default) =>
            inner.CountConsolidatedStockWithStockAsync(filter, cancellationToken);

        public Task<System.Collections.Generic.IReadOnlyList<OrderHub.Application.Features.Inventory.ConsolidatedBalances.ConsolidatedStockProjectionRow>> GetConsolidatedStockPageAsync(OrderHub.Application.Features.Inventory.ConsolidatedBalances.ConsolidatedStockQueryFilter filter, CancellationToken cancellationToken = default) =>
            inner.GetConsolidatedStockPageAsync(filter, cancellationToken);

        public Task<System.Collections.Generic.IReadOnlyList<OrderHub.Application.Features.Inventory.WarehouseStock.StockCategoryOption>> GetConsolidatedStockCategoriesAsync(System.Collections.Generic.IReadOnlyCollection<int> warehouseIds, CancellationToken cancellationToken = default) =>
            inner.GetConsolidatedStockCategoriesAsync(warehouseIds, cancellationToken);

        public Task<System.Collections.Generic.IReadOnlyList<OrderHub.Application.Features.Inventory.ConsolidatedBalances.ProductDistributionProjectionRow>> GetProductDistributionAsync(int productId, System.Collections.Generic.IReadOnlyCollection<int> warehouseIds, CancellationToken cancellationToken = default) =>
            inner.GetProductDistributionAsync(productId, warehouseIds, cancellationToken);

        public Task<InventoryMovement> GetMovementForUpdateAsync(int movementId, CancellationToken cancellationToken = default) =>
            inner.GetMovementForUpdateAsync(movementId, cancellationToken);
    }
}
