using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Warehouses;
using OrderHub.Application.Features.Warehouses.Contracts;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Features.Setup.Cities;
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
    private const string Street = "شارع الملك فهد";

    [Fact]
    public async Task Create_PersistsTrimmedActiveWarehouseWithAddress()
    {
        using var db = new WarehouseTestDb();
        var handler = new CreateWarehouseCommandHandler(
            db.Warehouses,
            db.Users,
            db.Cities,
            db.UnitOfWork);

        var result = await handler.Handle(
            new CreateWarehouseCommand(
                new("  المخزن الرئيسي  ", db.Keeper.Id, db.City.Id, $"  {Street}  ", "", "")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value > 0);

        db.Context.ChangeTracker.Clear();
        Warehouse saved = await db.Context.Warehouses
            .Include(warehouse => warehouse.Address)
            .ThenInclude(address => address.City)
            .SingleAsync();

        Assert.Equal("المخزن الرئيسي", saved.Name.Value);
        Assert.Equal(db.Keeper.Id, saved.KeeperUserId);
        Assert.Equal(db.City.Id, saved.Address.City.Id);
        Assert.Equal(Street, saved.Address.Street);
        Assert.Null(saved.Phone);
        Assert.True(saved.IsActive);
    }

    [Fact]
    public async Task Create_PersistsOptionalPhone()
    {
        using var db = new WarehouseTestDb();
        var handler = new CreateWarehouseCommandHandler(
            db.Warehouses,
            db.Users,
            db.Cities,
            db.UnitOfWork);

        var result = await handler.Handle(
            new CreateWarehouseCommand(
                new("مخزن الهاتف", db.Keeper.Id, db.City.Id, Street, "512345678", "+966")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        db.Context.ChangeTracker.Clear();
        Phone phone = await db.Context.Phones.SingleAsync();

        Assert.Equal("+966", phone.Number.CountryCode);
        Assert.Equal("512345678", phone.Number.NationalNumber);
    }

    [Fact]
    public async Task Create_RejectsInvalidPhone()
    {
        using var db = new WarehouseTestDb();
        var handler = new CreateWarehouseCommandHandler(
            db.Warehouses,
            db.Users,
            db.Cities,
            db.UnitOfWork);

        var result = await handler.Handle(
            new CreateWarehouseCommand(
                new("مخزن", db.Keeper.Id, db.City.Id, Street, "12", "+966")),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("رقم هاتف", result.ErrorMessage);
    }

    [Fact]
    public async Task Create_RejectsMissingCityOrStreet()
    {
        using var db = new WarehouseTestDb();
        var handler = new CreateWarehouseCommandHandler(
            db.Warehouses,
            db.Users,
            db.Cities,
            db.UnitOfWork);

        var missingStreet = await handler.Handle(
            new CreateWarehouseCommand(
                new("مخزن", db.Keeper.Id, db.City.Id, "   ", "", "")),
            CancellationToken.None);
        Assert.Equal("عنوان المخزن مطلوب ويجب ألا يتجاوز 150 حرفًا.", missingStreet.ErrorMessage);

        var missingCity = await handler.Handle(
            new CreateWarehouseCommand(
                new("مخزن", db.Keeper.Id, 0, Street, "", "")),
            CancellationToken.None);
        Assert.Equal("اختر المدينة.", missingCity.ErrorMessage);

        var unknownCity = await handler.Handle(
            new CreateWarehouseCommand(
                new("مخزن", db.Keeper.Id, 9999, Street, "", "")),
            CancellationToken.None);
        Assert.Equal("المدينة غير موجودة.", unknownCity.ErrorMessage);
    }

    [Fact]
    public async Task Create_RejectsCaseInsensitiveDuplicateName()
    {
        using var db = new WarehouseTestDb();
        var handler = new CreateWarehouseCommandHandler(
            db.Warehouses,
            db.Users,
            db.Cities,
            db.UnitOfWork);

        Assert.True((await handler.Handle(
            new CreateWarehouseCommand(
                new("Main Store", db.Keeper.Id, db.City.Id, Street, "", "")),
            CancellationToken.None)).IsSuccess);

        var duplicate = await handler.Handle(
            new CreateWarehouseCommand(
                new("main store", db.SecondKeeper.Id, db.City.Id, Street, "", "")),
            CancellationToken.None);

        Assert.False(duplicate.IsSuccess);
        Assert.Equal("اسم المخزن مستخدم بالفعل.", duplicate.ErrorMessage);
    }

    [Fact]
    public async Task Create_RejectsInvalidNameMissingInactiveOrIneligibleKeeper()
    {
        using var db = new WarehouseTestDb();
        var handler = new CreateWarehouseCommandHandler(
            db.Warehouses,
            db.Users,
            db.Cities,
            db.UnitOfWork);

        var shortName = await handler.Handle(
            new CreateWarehouseCommand(
                new("م", db.Keeper.Id, db.City.Id, Street, "", "")),
            CancellationToken.None);
        Assert.False(shortName.IsSuccess);

        var missingKeeper = await handler.Handle(
            new CreateWarehouseCommand(
                new("مخزن جديد", 9999, db.City.Id, Street, "", "")),
            CancellationToken.None);
        Assert.Equal("أمين المخزن غير موجود.", missingKeeper.ErrorMessage);

        var inactiveKeeper = await handler.Handle(
            new CreateWarehouseCommand(
                new("مخزن جديد", db.InactiveKeeper.Id, db.City.Id, Street, "", "")),
            CancellationToken.None);
        Assert.Equal("لا يمكن اختيار مستخدم غير نشط كأمين مخزن.", inactiveKeeper.ErrorMessage);

        var ineligibleKeeper = await handler.Handle(
            new CreateWarehouseCommand(
                new("مخزن جديد", db.KeeperWithoutInventoryWrite.Id, db.City.Id, Street, "", "")),
            CancellationToken.None);
        Assert.Equal(
            "لا يمكن اختيار مستخدم لا يملك صلاحية كتابة أو اعتماد المخزون كأمين مخزن.",
            ineligibleKeeper.ErrorMessage);
    }

    [Fact]
    public async Task Create_RejectsKeeperBoundToAnotherWarehouse()
    {
        using var db = new WarehouseTestDb();
        db.SeedWarehouse(keeper: db.Keeper);
        var handler = new CreateWarehouseCommandHandler(
            db.Warehouses,
            db.Users,
            db.Cities,
            db.UnitOfWork);

        var result = await handler.Handle(
            new CreateWarehouseCommand(
                new("مخزن ثانٍ", db.Keeper.Id, db.City.Id, Street, "", "")),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("هذا المستخدم أمين مخزن لمخزن آخر بالفعل.", result.ErrorMessage);
    }

    [Fact]
    public async Task GetForEdit_ProjectsContactAndLegacyNulls()
    {
        using var db = new WarehouseTestDb();
        Warehouse warehouse = db.SeedWarehouse(phoneNumber: "512345678");

        WarehouseEdit edit = await db.Warehouses.GetForEditAsync(warehouse.Id);

        Assert.NotNull(edit);
        Assert.Equal("أحمد المخزن", edit.KeeperDisplayName);
        Assert.Equal(db.City.Id, edit.CityId);
        Assert.Equal(Street, edit.Street);
        Assert.Equal("512345678", edit.PhoneNumber);
        Assert.Equal("+966", edit.CountryCode);

        await db.Context.Database.ExecuteSqlRawAsync(
            "UPDATE warehouses SET address_id = NULL, phone_id = NULL");
        db.Context.ChangeTracker.Clear();

        WarehouseEdit legacy = await db.Warehouses.GetForEditAsync(warehouse.Id);

        Assert.NotNull(legacy);
        Assert.Null(legacy.CityId);
        Assert.Equal(string.Empty, legacy.Street);
        Assert.Equal(string.Empty, legacy.PhoneNumber);
        Assert.Equal(string.Empty, legacy.CountryCode);
    }

    [Fact]
    public async Task Update_TransfersKeeperUpdatesAddressAndConsumersReadTheCurrentBinding()
    {
        using var db = new WarehouseTestDb();
        Warehouse warehouse = db.SeedWarehouse();
        var handler = new UpdateWarehouseCommandHandler(
            db.Warehouses,
            db.Users,
            db.Cities,
            db.UnitOfWork);

        var result = await handler.Handle(
            new UpdateWarehouseCommand(
                new(
                    warehouse.Id,
                    "المخزن الفرعي",
                    db.SecondKeeper.Id,
                    true,
                    db.SecondCity.Id,
                    "شارع التحلية",
                    "512345678",
                    "+966")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        db.Context.ChangeTracker.Clear();
        Warehouse saved = await db.Context.Warehouses
            .Include(item => item.Address)
            .ThenInclude(address => address.City)
            .Include(item => item.Phone)
            .SingleAsync();

        Assert.Equal("المخزن الفرعي", saved.Name.Value);
        Assert.Equal(db.SecondKeeper.Id, saved.KeeperUserId);
        Assert.Equal(db.SecondCity.Id, saved.Address.City.Id);
        Assert.Equal("شارع التحلية", saved.Address.Street);
        Assert.Equal("512345678", saved.Phone.Number.NationalNumber);

        var list = await db.Warehouses.GetListAsync();

        Assert.Single(list);
        Assert.Equal("سارة المخزن", list[0].KeeperDisplayName);
        Assert.Equal("جدة - شارع التحلية", list[0].Address);
        Assert.Equal("+966 512345678", list[0].PhoneNumber);
        Assert.Equal(0, list[0].PendingIssueOrderCount);
    }

    [Fact]
    public async Task Update_ClearsPhoneAndRemovesTheRow()
    {
        using var db = new WarehouseTestDb();
        Warehouse warehouse = db.SeedWarehouse(phoneNumber: "512345678");

        Assert.Single(db.Context.Phones);

        var handler = new UpdateWarehouseCommandHandler(
            db.Warehouses,
            db.Users,
            db.Cities,
            db.UnitOfWork);

        var result = await handler.Handle(
            new UpdateWarehouseCommand(
                new(warehouse.Id, warehouse.Name.Value, db.Keeper.Id, true, db.City.Id, Street, "", "")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        db.Context.ChangeTracker.Clear();
        Assert.Empty(await db.Context.Phones.ToListAsync());
        Assert.Single(await db.Context.Warehouses.ToListAsync());
    }

    [Fact]
    public async Task Update_RequiresAddressForLegacyWarehouse()
    {
        using var db = new WarehouseTestDb();
        Warehouse warehouse = db.SeedWarehouse();
        await db.Context.Database.ExecuteSqlRawAsync(
            "UPDATE warehouses SET address_id = NULL");
        db.Context.ChangeTracker.Clear();

        var handler = new UpdateWarehouseCommandHandler(
            db.Warehouses,
            db.Users,
            db.Cities,
            db.UnitOfWork);

        var missingAddress = await handler.Handle(
            new UpdateWarehouseCommand(
                new(warehouse.Id, warehouse.Name.Value, db.Keeper.Id, true, 0, "", "", "")),
            CancellationToken.None);

        Assert.Equal("عنوان المخزن مطلوب ويجب ألا يتجاوز 150 حرفًا.", missingAddress.ErrorMessage);

        var withAddress = await handler.Handle(
            new UpdateWarehouseCommand(
                new(warehouse.Id, warehouse.Name.Value, db.Keeper.Id, true, db.City.Id, Street, "", "")),
            CancellationToken.None);

        Assert.True(withAddress.IsSuccess);

        db.Context.ChangeTracker.Clear();
        Warehouse saved = await db.Context.Warehouses
            .Include(item => item.Address)
            .SingleAsync();

        Assert.NotNull(saved.Address);
        Assert.Equal(Street, saved.Address.Street);
    }

    [Fact]
    public async Task Update_RejectsKeeperOfAnotherWarehouse()
    {
        using var db = new WarehouseTestDb();
        db.SeedWarehouse("المخزن الأول", db.Keeper);
        Warehouse second = db.SeedWarehouse("المخزن الثاني", db.SecondKeeper);
        var handler = new UpdateWarehouseCommandHandler(
            db.Warehouses,
            db.Users,
            db.Cities,
            db.UnitOfWork);

        var result = await handler.Handle(
            new UpdateWarehouseCommand(
                new(second.Id, second.Name.Value, db.Keeper.Id, true, db.City.Id, Street, "", "")),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("هذا المستخدم أمين مخزن لمخزن آخر بالفعل.", result.ErrorMessage);
    }

    [Fact]
    public async Task Update_RejectsIneligibleKeeper()
    {
        using var db = new WarehouseTestDb();
        Warehouse warehouse = db.SeedWarehouse();
        var handler = new UpdateWarehouseCommandHandler(
            db.Warehouses,
            db.Users,
            db.Cities,
            db.UnitOfWork);

        var result = await handler.Handle(
            new UpdateWarehouseCommand(
                new(
                    warehouse.Id,
                    warehouse.Name.Value,
                    db.KeeperWithoutInventoryWrite.Id,
                    true,
                    db.City.Id,
                    Street,
                    "",
                    "")),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "لا يمكن اختيار مستخدم لا يملك صلاحية كتابة أو اعتماد المخزون كأمين مخزن.",
            result.ErrorMessage);
    }

    [Fact]
    public async Task Update_RejectsUnknownWarehouseAndInvalidName()
    {
        using var db = new WarehouseTestDb();
        var handler = new UpdateWarehouseCommandHandler(
            db.Warehouses,
            db.Users,
            db.Cities,
            db.UnitOfWork);

        var missing = await handler.Handle(
            new UpdateWarehouseCommand(
                new(12345, "مخزن", db.Keeper.Id, true, db.City.Id, Street, "", "")),
            CancellationToken.None);
        Assert.Equal("المخزن غير موجود.", missing.ErrorMessage);

        Warehouse warehouse = db.SeedWarehouse();
        var invalidName = await handler.Handle(
            new UpdateWarehouseCommand(
                new(warehouse.Id, "م", db.Keeper.Id, true, db.City.Id, Street, "", "")),
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
        var handler = new UpdateWarehouseCommandHandler(
            db.Warehouses,
            db.Users,
            db.Cities,
            db.UnitOfWork);

        var stockResult = await handler.Handle(
            new UpdateWarehouseCommand(
                new(withStock.Id, withStock.Name.Value, db.Keeper.Id, false, db.City.Id, Street, "", "")),
            CancellationToken.None);
        var damageResult = await handler.Handle(
            new UpdateWarehouseCommand(
                new(withDamage.Id, withDamage.Name.Value, db.SecondKeeper.Id, false, db.City.Id, Street, "", "")),
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
        var handler = new UpdateWarehouseCommandHandler(
            db.Warehouses,
            db.Users,
            db.Cities,
            db.UnitOfWork);

        var deactivate = await handler.Handle(
            new UpdateWarehouseCommand(
                new(warehouse.Id, warehouse.Name.Value, db.Keeper.Id, false, db.City.Id, Street, "", "")),
            CancellationToken.None);

        Assert.True(deactivate.IsSuccess);

        db.Context.ChangeTracker.Clear();
        Assert.False((await db.Context.Warehouses.SingleAsync()).IsActive);

        var activate = await handler.Handle(
            new UpdateWarehouseCommand(
                new(warehouse.Id, warehouse.Name.Value, db.Keeper.Id, true, db.City.Id, Street, "", "")),
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
        db.Context.Attach(db.City);
        db.Context.Warehouses.Add(
            new Warehouse("مخزن مكرر", db.Keeper.Id, db.City, Street));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Database_RejectsDuplicateWarehouseName()
    {
        using var db = new WarehouseTestDb();
        db.SeedWarehouse("المخزن الرئيسي", db.Keeper);
        db.Context.Attach(db.City);
        db.Context.Warehouses.Add(
            new Warehouse("المخزن الرئيسي", db.SecondKeeper.Id, db.City, Street));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.Context.SaveChangesAsync());
    }

    [Fact]
    public void CommandsRequireWarehousesWriteAndQueriesRequireRead()
    {
        AssertPermission<CreateWarehouseCommand>(PermissionLevel.Write);
        AssertPermission<UpdateWarehouseCommand>(PermissionLevel.Write);
        AssertPermission<GetWarehouseEditorCitiesQuery>(PermissionLevel.Write);
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
    private const string Street = "شارع الملك فهد";

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
        Cities = new CityRepository(Context);

        City = new City("الرياض");
        SecondCity = new City("جدة");
        Context.Cities.AddRange(City, SecondCity);

        var storekeeperRole = new Role("أمين مخزن");
        storekeeperRole.SetPermission(PermissionFeature.Inventory, PermissionLevel.Write);
        Context.Roles.Add(storekeeperRole);

        var salesRole = new Role("بائع");
        salesRole.SetPermission(PermissionFeature.Inventory, PermissionLevel.Read);
        Context.Roles.Add(salesRole);

        Keeper = User.Create("keeper1", "hash", "أحمد المخزن");
        Keeper.AssignRole(storekeeperRole);

        SecondKeeper = User.Create("keeper2", "hash", "سارة المخزن");
        SecondKeeper.AssignRole(storekeeperRole);

        InactiveKeeper = User.Create("keeper3", "hash", "موظف موقوف", isActive: false);
        InactiveKeeper.AssignRole(storekeeperRole);

        KeeperWithoutInventoryWrite = User.Create("keeper4", "hash", "بائع مخزن");
        KeeperWithoutInventoryWrite.AssignRole(salesRole);

        Context.Users.AddRange(
            Keeper,
            SecondKeeper,
            InactiveKeeper,
            KeeperWithoutInventoryWrite);

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

    public CityRepository Cities { get; }

    public City City { get; }

    public City SecondCity { get; }

    public User Keeper { get; }

    public User SecondKeeper { get; }

    public User InactiveKeeper { get; }

    public User KeeperWithoutInventoryWrite { get; }

    public Product Product { get; }

    public Warehouse SeedWarehouse(
        string name = "المخزن الرئيسي",
        User? keeper = null,
        string phoneNumber = null)
    {
        Context.Attach(City);

        var warehouse = new Warehouse(
            name,
            (keeper ?? Keeper).Id,
            City,
            Street,
            phoneNumber,
            "+966");

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
