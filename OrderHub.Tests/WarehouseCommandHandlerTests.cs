using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Warehouses;
using OrderHub.Application.Features.Warehouses.Contracts;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Reopsitories;
using OrderHub.Infrastructure.Services;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OrderHub.Tests;

public sealed class WarehouseCommandHandlerTests
{
    [Fact]
    public async Task Create_PersistsTrimmedActiveWarehouse()
    {
        using var db = new WarehouseTestDb();
        var handler = new CreateWarehouseCommandHandler(db.Warehouses, db.Users, db.UnitOfWork);

        var result = await handler.Handle(
            new CreateWarehouseCommand(new("  المخزن الرئيسي  ", db.Keeper.Id)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value > 0);

        db.Context.ChangeTracker.Clear();
        Warehouse saved = await db.Context.Warehouses.SingleAsync();

        Assert.Equal("المخزن الرئيسي", saved.Name.Value);
        Assert.Equal(db.Keeper.Id, saved.KeeperUserId);
        Assert.True(saved.IsActive);
    }

    [Fact]
    public async Task Create_RejectsCaseInsensitiveDuplicateName()
    {
        using var db = new WarehouseTestDb();
        var handler = new CreateWarehouseCommandHandler(db.Warehouses, db.Users, db.UnitOfWork);

        Assert.True((await handler.Handle(
            new CreateWarehouseCommand(new("Main Store", db.Keeper.Id)),
            CancellationToken.None)).IsSuccess);

        var duplicate = await handler.Handle(
            new CreateWarehouseCommand(new("main store", db.SecondKeeper.Id)),
            CancellationToken.None);

        Assert.False(duplicate.IsSuccess);
        Assert.Equal("اسم المخزن مستخدم بالفعل.", duplicate.ErrorMessage);
    }

    [Fact]
    public async Task Create_RejectsInvalidNameMissingKeeperAndInactiveKeeper()
    {
        using var db = new WarehouseTestDb();
        var handler = new CreateWarehouseCommandHandler(db.Warehouses, db.Users, db.UnitOfWork);

        var shortName = await handler.Handle(
            new CreateWarehouseCommand(new("م", db.Keeper.Id)),
            CancellationToken.None);
        Assert.False(shortName.IsSuccess);

        var missingKeeper = await handler.Handle(
            new CreateWarehouseCommand(new("مخزن جديد", 9999)),
            CancellationToken.None);
        Assert.Equal("أمين المخزن غير موجود.", missingKeeper.ErrorMessage);

        var inactiveKeeper = await handler.Handle(
            new CreateWarehouseCommand(new("مخزن جديد", db.InactiveKeeper.Id)),
            CancellationToken.None);
        Assert.Equal("لا يمكن اختيار مستخدم غير نشط كأمين مخزن.", inactiveKeeper.ErrorMessage);
    }

    [Fact]
    public async Task Create_RejectsKeeperBoundToAnotherWarehouse()
    {
        using var db = new WarehouseTestDb();
        db.SeedWarehouse(keeper: db.Keeper);
        var handler = new CreateWarehouseCommandHandler(db.Warehouses, db.Users, db.UnitOfWork);

        var result = await handler.Handle(
            new CreateWarehouseCommand(new("مخزن ثانٍ", db.Keeper.Id)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("هذا المستخدم أمين مخزن لمخزن آخر بالفعل.", result.ErrorMessage);
    }

    [Fact]
    public async Task Update_TransfersKeeperAndConsumersReadTheCurrentBinding()
    {
        using var db = new WarehouseTestDb();
        Warehouse warehouse = db.SeedWarehouse();
        var handler = new UpdateWarehouseCommandHandler(db.Warehouses, db.Users, db.UnitOfWork);

        var result = await handler.Handle(
            new UpdateWarehouseCommand(
                new(warehouse.Id, "المخزن الفرعي", db.SecondKeeper.Id, true)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        db.Context.ChangeTracker.Clear();
        Warehouse saved = await db.Context.Warehouses.SingleAsync();

        Assert.Equal("المخزن الفرعي", saved.Name.Value);
        Assert.Equal(db.SecondKeeper.Id, saved.KeeperUserId);

        var list = await db.Warehouses.GetListAsync();

        Assert.Single(list);
        Assert.Equal("سارة المخزن", list[0].KeeperDisplayName);
        Assert.Equal(0, list[0].PendingIssueOrderCount);
    }

    [Fact]
    public async Task Update_RejectsKeeperOfAnotherWarehouse()
    {
        using var db = new WarehouseTestDb();
        db.SeedWarehouse("المخزن الأول", db.Keeper);
        Warehouse second = db.SeedWarehouse("المخزن الثاني", db.SecondKeeper);
        var handler = new UpdateWarehouseCommandHandler(db.Warehouses, db.Users, db.UnitOfWork);

        var result = await handler.Handle(
            new UpdateWarehouseCommand(
                new(second.Id, second.Name.Value, db.Keeper.Id, true)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("هذا المستخدم أمين مخزن لمخزن آخر بالفعل.", result.ErrorMessage);
    }

    [Fact]
    public async Task Update_RejectsUnknownWarehouseAndInvalidName()
    {
        using var db = new WarehouseTestDb();
        var handler = new UpdateWarehouseCommandHandler(db.Warehouses, db.Users, db.UnitOfWork);

        var missing = await handler.Handle(
            new UpdateWarehouseCommand(new(12345, "مخزن", db.Keeper.Id, true)),
            CancellationToken.None);
        Assert.Equal("المخزن غير موجود.", missing.ErrorMessage);

        Warehouse warehouse = db.SeedWarehouse();
        var invalidName = await handler.Handle(
            new UpdateWarehouseCommand(new(warehouse.Id, "م", db.Keeper.Id, true)),
            CancellationToken.None);
        Assert.False(invalidName.IsSuccess);
    }

    [Fact]
    public async Task Update_BlocksDeactivationWhileStockOrDamagedQuantitiesArePositive()
    {
        using var db = new WarehouseTestDb();
        Warehouse withStock = db.SeedWarehouse("مخزن برصيد", db.Keeper);
        Warehouse withDamage = db.SeedWarehouse("مخزن تالف", db.SecondKeeper);
        db.SeedStock(withStock.Id, quantity: 5);
        db.SeedStock(withDamage.Id, damaged: 2);
        var handler = new UpdateWarehouseCommandHandler(db.Warehouses, db.Users, db.UnitOfWork);

        var stockResult = await handler.Handle(
            new UpdateWarehouseCommand(new(withStock.Id, withStock.Name.Value, db.Keeper.Id, false)),
            CancellationToken.None);
        var damageResult = await handler.Handle(
            new UpdateWarehouseCommand(new(withDamage.Id, withDamage.Name.Value, db.SecondKeeper.Id, false)),
            CancellationToken.None);

        Assert.False(stockResult.IsSuccess);
        Assert.Contains("أرصدة", stockResult.ErrorMessage);
        Assert.False(damageResult.IsSuccess);
        Assert.Contains("أرصدة", damageResult.ErrorMessage);

        db.Context.ChangeTracker.Clear();
        Assert.All(await db.Context.Warehouses.ToListAsync(), warehouse => Assert.True(warehouse.IsActive));
    }

    [Fact]
    public async Task Update_DeactivatesAndReactivatesWhenStockIsZero()
    {
        using var db = new WarehouseTestDb();
        Warehouse warehouse = db.SeedWarehouse();
        db.SeedStock(warehouse.Id);
        var handler = new UpdateWarehouseCommandHandler(db.Warehouses, db.Users, db.UnitOfWork);

        var deactivate = await handler.Handle(
            new UpdateWarehouseCommand(new(warehouse.Id, warehouse.Name.Value, db.Keeper.Id, false)),
            CancellationToken.None);

        Assert.True(deactivate.IsSuccess);

        db.Context.ChangeTracker.Clear();
        Assert.False((await db.Context.Warehouses.SingleAsync()).IsActive);

        var activate = await handler.Handle(
            new UpdateWarehouseCommand(new(warehouse.Id, warehouse.Name.Value, db.Keeper.Id, true)),
            CancellationToken.None);

        Assert.True(activate.IsSuccess);

        db.Context.ChangeTracker.Clear();
        Assert.True((await db.Context.Warehouses.SingleAsync()).IsActive);
    }

    [Fact]
    public async Task Database_RejectsTwoWarehousesForOneKeeper()
    {
        using var db = new WarehouseTestDb();
        db.SeedWarehouse(keeper: db.Keeper);
        db.Context.Warehouses.Add(new Warehouse("مخزن مكرر", db.Keeper.Id));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Database_RejectsDuplicateWarehouseName()
    {
        using var db = new WarehouseTestDb();
        db.SeedWarehouse("المخزن الرئيسي", db.Keeper);
        db.Context.Warehouses.Add(new Warehouse("المخزن الرئيسي", db.SecondKeeper.Id));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.Context.SaveChangesAsync());
    }

    [Fact]
    public void CommandsRequireWarehousesWriteAndQueriesRequireRead()
    {
        AssertPermission<CreateWarehouseCommand>(PermissionLevel.Write);
        AssertPermission<UpdateWarehouseCommand>(PermissionLevel.Write);
        AssertPermission<GetWarehousesQuery>(PermissionLevel.Read);
        AssertPermission<GetWarehouseQuery>(PermissionLevel.Read);
    }

    private static void AssertPermission<TRequest>(PermissionLevel level)
    {
        RequirePermissionAttribute? attribute = typeof(TRequest)
            .GetCustomAttribute<RequirePermissionAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(PermissionFeature.Warehouses, attribute.Feature);
        Assert.Equal(level, attribute.RequiredLevel);
    }
}

internal sealed class WarehouseTestDb : IDisposable
{
    private readonly SqliteConnection _connection;

    public WarehouseTestDb()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .UseSnakeCaseNamingConvention()
            .Options;

        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();

        UnitOfWork = new UnitOfWork(
            Context,
            new NumberSequenceService(Context, new AppDbContextFactory(options)));

        Warehouses = new WarehouseRepository(Context);
        Users = new UserRepository(Context);

        Keeper = User.Create("keeper1", "hash", "أحمد المخزن");
        SecondKeeper = User.Create("keeper2", "hash", "سارة المخزن");
        InactiveKeeper = User.Create("keeper3", "hash", "موظف موقوف", isActive: false);
        Context.Users.AddRange(Keeper, SecondKeeper, InactiveKeeper);

        var category = new Category("مستلزمات");
        Product = new Product("قفاز طبي", "GLV-001", category, new Money(5m));
        Context.Categories.Add(category);
        Context.Products.Add(Product);

        Context.SaveChanges();
        Context.ChangeTracker.Clear();
    }

    public AppDbContext Context { get; }

    public UnitOfWork UnitOfWork { get; }

    public WarehouseRepository Warehouses { get; }

    public UserRepository Users { get; }

    public User Keeper { get; }

    public User SecondKeeper { get; }

    public User InactiveKeeper { get; }

    public Product Product { get; }

    public Warehouse SeedWarehouse(string name = "المخزن الرئيسي", User? keeper = null)
    {
        var warehouse = new Warehouse(name, (keeper ?? Keeper).Id);
        Context.Warehouses.Add(warehouse);
        Context.SaveChanges();
        Context.ChangeTracker.Clear();
        return warehouse;
    }

    public void SeedStock(int warehouseId, int quantity = 0, int damaged = 0)
    {
        var balance = StockBalance.Create(warehouseId, Product.Id);

        if (quantity > 0)
            balance.ApplyIn(quantity);

        if (damaged > 0)
            balance.DamagedIn(damaged);

        Context.StockBalances.Add(balance);
        Context.SaveChanges();
        Context.ChangeTracker.Clear();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
