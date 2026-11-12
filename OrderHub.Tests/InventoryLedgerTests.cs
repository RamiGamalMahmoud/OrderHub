using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Reopsitories;
using Xunit;

namespace OrderHub.Tests;

public sealed class InventoryLedgerTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly InventoryRepository _repository;
    private readonly Product _product;
    private readonly User _user;

    public InventoryLedgerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .UseSnakeCaseNamingConvention()
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
        _repository = new InventoryRepository(_context);

        var category = new Category("مستلزمات");
        _product = new Product("قفاز طبي", "GLV-001", category, new Money(12.5m));
        _user = User.Create("tester", "hash", "أمين مخزن");

        _context.Categories.Add(category);
        _context.Products.Add(_product);
        _context.Users.Add(_user);
        _context.SaveChanges();
    }

    // ---------- factory validation ----------

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Create_RejectsNonPositiveQuantity(int quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Movement(quantity: quantity));
    }

    [Fact]
    public void Create_RejectsFutureEffectiveTime()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Movement(effectiveAtUtc: Now.AddMinutes(10)));
    }

    [Fact]
    public void Create_AllowsSmallClockMargin()
    {
        var movement = Movement(effectiveAtUtc: Now.AddMinutes(3));

        Assert.Equal(Now.AddMinutes(3), movement.EffectiveAtUtc);
        Assert.Equal(InventoryMovementStatus.Active, movement.Status);
        Assert.Equal(Now, movement.CreatedAtUtc);
    }

    [Fact]
    public void Create_RejectsMissingDefaultEffectiveTime()
    {
        Assert.Throws<ArgumentException>(() => Movement(effectiveAtUtc: default(DateTime)));
    }

    [Fact]
    public void Create_RejectsDirectionMismatch()
    {
        Assert.Throws<ArgumentException>(
            () => Movement(type: InventoryMovementType.StockIn, direction: InventoryDirection.Out));
    }

    [Fact]
    public void Create_RequiresReversalReferenceOnlyForReversals()
    {
        Assert.Throws<ArgumentException>(
            () => Movement(type: InventoryMovementType.Reversal, note: "عكس"));

        Assert.Throws<ArgumentException>(
            () => Movement(type: InventoryMovementType.StockIn, reversalOfMovementId: 7));
    }

    [Fact]
    public void Create_RequiresNoteForSensitiveMovements()
    {
        Assert.Throws<ArgumentException>(
            () => Movement(type: InventoryMovementType.Adjustment, note: "  "));

        Assert.Throws<ArgumentException>(
            () => Movement(
                type: InventoryMovementType.Reversal,
                reversalOfMovementId: 7));

        Assert.Throws<ArgumentException>(
            () => Movement(type: InventoryMovementType.DamagedAdjustment, direction: InventoryDirection.Out));
    }

    [Fact]
    public void Create_TrimsNoteAndRejectsOverlongNotes()
    {
        var movement = Movement(type: InventoryMovementType.Adjustment, note: "  تسوية افتتاحية  ");

        Assert.Equal("تسوية افتتاحية", movement.Note);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => Movement(type: InventoryMovementType.Adjustment, note: new string('ن', 501)));
    }

    [Fact]
    public void Create_RejectsNonPositiveIdentifiers()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => InventoryMovement.Create(0, _product.Id, 1, InventoryDirection.In,
                InventoryMovementType.StockIn, Now, _user.Id, nowUtc: Now));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => InventoryMovement.Create(1, _product.Id, 1, InventoryDirection.In,
                InventoryMovementType.StockIn, Now, 0, nowUtc: Now));
    }

    // ---------- immutability ----------

    [Fact]
    public void MarkReversed_IsInternalOnly()
    {
        var method = typeof(InventoryMovement).GetMethod(
            "MarkReversed",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        Assert.True(method!.IsAssembly);
    }

    [Fact]
    public void MarkReversed_CannotBeAppliedTwice()
    {
        var movement = Movement();

        movement.MarkReversed();

        Assert.Equal(InventoryMovementStatus.Reversed, movement.Status);
        Assert.Throws<InvalidOperationException>(() => movement.MarkReversed());
    }

    // ---------- balance behaviors ----------

    [Fact]
    public void Balance_MovesWithinInvariantsAndBumpsVersion()
    {
        var balance = StockBalance.Create(1, _product.Id);

        balance.ApplyIn(10);
        balance.Reserve(4);
        Assert.Equal((10, 4, 6, 2), (balance.StockOnHand, balance.ReservedOnHand, balance.Available, balance.Version));

        Assert.Throws<InvalidOperationException>(() => balance.ApplyOut(7));
        balance.ApplyOut(6);
        Assert.Equal((4, 4, 0, 3), (balance.StockOnHand, balance.ReservedOnHand, balance.Available, balance.Version));

        Assert.Throws<InvalidOperationException>(() => balance.Release(5));
        balance.Release(1);
        Assert.Equal((4, 3, 1, 4), (balance.StockOnHand, balance.ReservedOnHand, balance.Available, balance.Version));

        balance.Consume(3);
        Assert.Equal((1, 0, 1, 5), (balance.StockOnHand, balance.ReservedOnHand, balance.Available, balance.Version));
    }

    [Fact]
    public void Balance_ReserveCannotExceedAvailable()
    {
        var balance = StockBalance.Create(1, _product.Id);
        balance.ApplyIn(5);

        Assert.Throws<InvalidOperationException>(() => balance.Reserve(6));
        balance.Reserve(5);
        Assert.Throws<InvalidOperationException>(() => balance.Reserve(1));
    }

    [Fact]
    public void Balance_DamagedNeverTouchesStockOrAvailable()
    {
        var balance = StockBalance.Create(1, _product.Id);
        balance.ApplyIn(10);

        balance.DamagedIn(3);
        Assert.Equal((10, 0, 3), (balance.StockOnHand, balance.ReservedOnHand, balance.DamagedQty));

        Assert.Throws<InvalidOperationException>(() => balance.DamagedDisposal(4));
        balance.DamagedDisposal(2);
        Assert.Equal((10, 1), (balance.StockOnHand, balance.DamagedQty));

        balance.DamagedAdjust(2);
        Assert.Equal(3, balance.DamagedQty);

        Assert.Throws<InvalidOperationException>(() => balance.DamagedAdjust(-4));
        Assert.Throws<ArgumentOutOfRangeException>(() => balance.DamagedAdjust(0));
    }

    // ---------- repository CAS operations ----------

    [Fact]
    public async Task TryReserve_IsAtomicAndGuardFailureChangesNothing()
    {
        await SeedStockAsync(10);

        Assert.True(await _repository.TryReserveAsync(1, _product.Id, 6));
        var balance = await ReloadStockAsync();
        Assert.Equal((10, 6, 2), (balance!.StockOnHand, balance.ReservedOnHand, balance.Version));

        Assert.False(await _repository.TryReserveAsync(1, _product.Id, 5));
        balance = await ReloadStockAsync();
        Assert.Equal((10, 6, 2), (balance!.StockOnHand, balance.ReservedOnHand, balance.Version));
    }

    [Fact]
    public async Task Release_IsAtomicAndGuardFailureChangesNothing()
    {
        await SeedStockAsync(10);
        Assert.True(await _repository.TryReserveAsync(1, _product.Id, 4));
        _context.ChangeTracker.Clear();

        Assert.False(await _repository.ReleaseAsync(1, _product.Id, 5));
        var balance = await ReloadStockAsync();
        Assert.Equal((10, 4), (balance!.StockOnHand, balance.ReservedOnHand));

        Assert.True(await _repository.ReleaseAsync(1, _product.Id, 4));
        balance = await ReloadStockAsync();
        Assert.Equal((10, 0, 3), (balance!.StockOnHand, balance.ReservedOnHand, balance.Version));
    }

    [Fact]
    public async Task Consume_RemovesStockAndReservationTogether()
    {
        await SeedStockAsync(10);
        Assert.True(await _repository.TryReserveAsync(1, _product.Id, 10));
        _context.ChangeTracker.Clear();

        Assert.False(await _repository.ConsumeAsync(1, _product.Id, 11));
        var balance = await ReloadStockAsync();
        Assert.Equal((10, 10), (balance!.StockOnHand, balance.ReservedOnHand));

        Assert.True(await _repository.ConsumeAsync(1, _product.Id, 4));
        balance = await ReloadStockAsync();
        Assert.Equal((6, 6, 0), (balance!.StockOnHand, balance.ReservedOnHand, balance.Available));

        Assert.True(await _repository.ConsumeAsync(1, _product.Id, 6));
        balance = await ReloadStockAsync();
        Assert.Equal((0, 0, 4), (balance!.StockOnHand, balance.ReservedOnHand, balance.Version));
    }

    [Fact]
    public async Task AtomicOperations_ReturnFalseWhenNoBalanceRowExists()
    {
        Assert.False(await _repository.TryReserveAsync(9, _product.Id, 1));
        Assert.False(await _repository.ReleaseAsync(9, _product.Id, 1));
        Assert.False(await _repository.ConsumeAsync(9, _product.Id, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task AtomicOperations_RejectNonPositiveQuantity(int quantity)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _repository.TryReserveAsync(1, _product.Id, quantity));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _repository.ReleaseAsync(1, _product.Id, quantity));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _repository.ConsumeAsync(1, _product.Id, quantity));
    }

    // ---------- persistence ----------

    [Fact]
    public async Task Movement_RoundTripsWithStringEnums()
    {
        var movement = Movement(type: InventoryMovementType.StockIn, quantity: 7);
        _repository.Add(movement);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var stored = await _context.InventoryMovements.AsNoTracking().SingleAsync();

        Assert.Equal((1, _product.Id, 7, _user.Id), (stored.WarehouseId, stored.ProductId, stored.Quantity, stored.CreatedByUserId));
        Assert.Equal(InventoryDirection.In, stored.Direction);
        Assert.Equal(InventoryMovementType.StockIn, stored.MovementType);
        Assert.Equal(InventoryMovementStatus.Active, stored.Status);
        Assert.Null(stored.ReversalOfMovementId);

        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT direction, movement_type, status FROM inventory_movements";
        using var reader = command.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Equal(("In", "StockIn", "Active"), (reader.GetString(0), reader.GetString(1), reader.GetString(2)));
    }

    [Fact]
    public async Task StockBalance_RejectsDuplicateWarehouseProductPair()
    {
        await SeedStockAsync(1);

        _context.StockBalances.Add(StockBalance.Create(1, _product.Id));

        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Ledger_RejectsSecondReversalOfSameMovement()
    {
        var original = Movement(type: InventoryMovementType.StockIn, quantity: 3);
        _repository.Add(original);
        await _context.SaveChangesAsync();

        var first = Movement(
            type: InventoryMovementType.Reversal,
            direction: InventoryDirection.Out,
            note: "عكس أول",
            reversalOfMovementId: original.Id);
        _repository.Add(first);
        await _context.SaveChangesAsync();

        var second = Movement(
            type: InventoryMovementType.Reversal,
            direction: InventoryDirection.In,
            note: "عكس ثان",
            reversalOfMovementId: original.Id);
        _repository.Add(second);

        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task ProtectHistory_RejectsModificationAndDeletion()
    {
        var movement = Movement(type: InventoryMovementType.Adjustment, note: "تسوية");
        _repository.Add(movement);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var tracked = await _context.InventoryMovements.SingleAsync();
        _context.Entry(tracked).Property(nameof(InventoryMovement.Note)).CurrentValue = "تعديل";

        await Assert.ThrowsAsync<InvalidOperationException>(() => _context.SaveChangesAsync());

        _context.ChangeTracker.Clear();
        tracked = await _context.InventoryMovements.SingleAsync();
        _context.InventoryMovements.Remove(tracked);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task ProtectHistory_AllowsInternalStatusTransitionOnly()
    {
        var movement = Movement(type: InventoryMovementType.StockIn, quantity: 2);
        _repository.Add(movement);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var tracked = await _context.InventoryMovements.SingleAsync();
        tracked.MarkReversed();
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();
        Assert.Equal(InventoryMovementStatus.Reversed,
            (await _context.InventoryMovements.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task Ledger_IsOrderedByEffectiveTimeDescending()
    {
        _repository.Add(Movement(effectiveAtUtc: Now.AddHours(-2), quantity: 1));
        _repository.Add(Movement(effectiveAtUtc: Now, quantity: 3));
        _repository.Add(Movement(effectiveAtUtc: Now.AddHours(-1), quantity: 2));
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var ledger = await _repository.GetLedgerAsync(1, _product.Id, skip: 0, take: 10);

        Assert.Equal(new[] { 3, 2, 1 }, ledger.Select(x => x.Quantity));
        Assert.Equal(3, await _repository.CountLedgerAsync(1, _product.Id));
        Assert.Equal(3, await _repository.CountLedgerAsync(1, null));
        Assert.Empty(await _repository.GetLedgerAsync(2, null, skip: 0, take: 10));
    }

    // ---------- helpers ----------

    private InventoryMovement Movement(
        InventoryMovementType type = InventoryMovementType.StockIn,
        InventoryDirection direction = InventoryDirection.In,
        int quantity = 5,
        string? note = null,
        DateTime? effectiveAtUtc = null,
        int? reversalOfMovementId = null)
    {
        return InventoryMovement.Create(
            1,
            _product.Id,
            quantity,
            direction,
            type,
            effectiveAtUtc ?? Now,
            _user.Id,
            note,
            reversalOfMovementId,
            nowUtc: Now);
    }

    private async Task<StockBalance> SeedStockAsync(int quantity)
    {
        var balance = await _repository.GetOrCreateStockItemAsync(1, _product.Id);
        balance.ApplyIn(quantity);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return balance;
    }

    private async Task<StockBalance> ReloadStockAsync()
    {
        _context.ChangeTracker.Clear();
        return await _repository.GetStockItemAsync(1, _product.Id);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
