using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Context;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Reopsitories;
using OrderHub.Infrastructure.Services;
using System;
using System.Threading.Tasks;

namespace OrderHub.Tests;

/// <summary>
/// In-memory SQLite context with a client, two warehouses, stock, a supplier,
/// products and the order/inventory repositories used by allocation tests.
/// </summary>
internal sealed class OrderAllocationTestDb : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 12, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;

    public OrderAllocationTestDb()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .UseSnakeCaseNamingConvention()
            .Options;

        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();
        Factory = new AppDbContextFactory(options);
        Clock = new FixedClock(Now);
        Numbering = new FakeNumberSequenceService();
        UnitOfWork = new UnitOfWork(Context, Numbering);
        Orders = new OrderRepository(Context);
        Inventory = new InventoryRepository(Context);
        Warehouses = new WarehouseRepository(Context);
        IssueOrders = new StockIssueOrderRepository(Context);
        Approvals = new InventoryApprovalRepository(Context);
        Counts = new InventoryCountRepository(Context);

        Seed();
    }

    public AppDbContext Context { get; }

    public AppDbContextFactory Factory { get; }

    public FixedClock Clock { get; }

    public FakeNumberSequenceService Numbering { get; }

    public UnitOfWork UnitOfWork { get; }

    public OrderRepository Orders { get; }

    public InventoryRepository Inventory { get; }

    public WarehouseRepository Warehouses { get; }

    public StockIssueOrderRepository IssueOrders { get; }

    public InventoryApprovalRepository Approvals { get; }

    public InventoryCountRepository Counts { get; }

    public City City { get; private set; } = null!;

    public User Keeper { get; private set; } = null!;

    public User Sales { get; private set; } = null!;

    public Warehouse MainWarehouse { get; private set; } = null!;

    public Warehouse SecondWarehouse { get; private set; } = null!;

    public Warehouse InactiveWarehouse { get; private set; } = null!;

    public Supplier Supplier { get; private set; } = null!;

    public Category Category { get; private set; } = null!;

    public Product Product { get; private set; } = null!;

    public Product SecondProduct { get; private set; } = null!;

    public Client Client { get; private set; } = null!;

    public StockBalance StockIn(Warehouse warehouse, Product product, int quantity)
    {
        var balance = StockBalance.Create(warehouse.Id, product.Id);
        balance.ApplyIn(quantity);
        Context.StockBalances.Add(balance);
        Context.SaveChanges();
        Context.ChangeTracker.Clear();
        return balance;
    }

    public StockBalance ReloadBalance(Warehouse warehouse, Product product) =>
        Context.StockBalances.AsNoTracking()
            .SingleOrDefault(balance => balance.WarehouseId == warehouse.Id && balance.ProductId == product.Id)!;

    public Order NewOrder(string number, params (Product Product, int Quantity)[] items)
    {
        var order = new Order(Client.Id, number);
        order.UpdateHeader(Client.Id, OrderSource.Online, DeliveryMethod.Pickup, null, null, null);
        foreach (var (product, quantity) in items)
            order.AddOrderItem(product.Id, product.Name.Value, 10m, quantity, 0m, null, null, []);
        Context.Orders.Add(order);
        Context.SaveChanges();
        Context.ChangeTracker.Clear();
        return order;
    }

    public void SeedReservation(Warehouse warehouse, Product product, int quantity)
    {
        var balance = Context.StockBalances
            .Single(row => row.WarehouseId == warehouse.Id && row.ProductId == product.Id);
        balance.Reserve(quantity);
        Context.SaveChanges();
        Context.ChangeTracker.Clear();
    }

    private void Seed()
    {
        City = new City("الرياض");
        Context.Cities.Add(City);

        Keeper = User.Create("keeper1", "hash", "أمين المخزن");
        var secondKeeper = User.Create("keeper2", "hash", "أمين المخزن الثاني");
        var retiredKeeper = User.Create("keeper3", "hash", "أمين المخزن السابق");
        Sales = User.Create("sales1", "hash", "مبيعات");
        Context.Users.AddRange(Keeper, secondKeeper, retiredKeeper, Sales);
        Context.SaveChanges();

        MainWarehouse = new Warehouse("المخزن الرئيسي", Keeper.Id, City, "شارع الملك فهد");
        SecondWarehouse = new Warehouse("المخزن الثاني", secondKeeper.Id, City, "طريق العروبة");
        InactiveWarehouse = new Warehouse("مخزن قديم", retiredKeeper.Id, City, "شارع العليا");
        InactiveWarehouse.Deactivate();
        Context.Warehouses.AddRange(MainWarehouse, SecondWarehouse, InactiveWarehouse);

        Supplier = new Supplier("مورد الجملة");
        Context.Suppliers.Add(Supplier);

        Category = new Category("مستلزمات");
        Context.Categories.Add(Category);

        Product = new Product("قفاز طبي", "GLV-001", Category, new Money(10m));
        Product.AddSupplier(Supplier);
        SecondProduct = new Product("كمامة طبية", "MSK-001", Category, new Money(5m));
        SecondProduct.AddSupplier(Supplier);
        Context.Products.AddRange(Product, SecondProduct);

        Client = new Client("عميل نقدي", "شارع التحلية", City, "0501111111", "+966");
        Context.Clients.Add(Client);

        Context.SaveChanges();
        Context.ChangeTracker.Clear();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
