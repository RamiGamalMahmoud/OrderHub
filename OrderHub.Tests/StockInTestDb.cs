using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderHub.Application.Authorization;
using OrderHub.Application.Context;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Features.Administration;
using OrderHub.Infrastructure.Features.Products;
using OrderHub.Infrastructure.Options;
using OrderHub.Infrastructure.Reopsitories;
using OrderHub.Infrastructure.Services;
using OrderHub.Infrastructure.Services.FilesAccess;
using System;
using System.IO;

namespace OrderHub.Tests;

/// <summary>
/// PostgreSQL test database seeded with a keeper, warehouses, products (one with a
/// supplier link, one without), and the temp stock-attachments folder used by D14.
/// </summary>
internal sealed class StockInTestDb : IDisposable
{
    public StockInTestDb()
    {
        PostgresTestDatabase.Reset();

        var options = PostgresTestDatabase.Options;

        Context = new AppDbContext(options);
        Factory = new AppDbContextFactory(options);

        UnitOfWork = new UnitOfWork(
            Context,
            new NumberSequenceService(Context, Factory));

        Warehouses = new WarehouseRepository(Context);
        Inventory = new InventoryRepository(Context);
        Receipts = new StockReceiptRepository(Context);
        Products = new ProductRepository(Context);
        Users = new UserRepository(Context);

        RootDirectory = Path.Combine(
            Path.GetTempPath(),
            "orderhub-stock-tests",
            Guid.NewGuid().ToString("N"));
        Directories = new ApplicationDirectoriesService(
            Options.Create(new ApplicationDataOptions { RootPath = RootDirectory }));
        Directories.EnsureDirectoriesCreated();
        Files = new LocalFileAccessService(Directories);

        Seed();
    }

    public AppDbContext Context { get; }

    public AppDbContextFactory Factory { get; }

    public UnitOfWork UnitOfWork { get; }

    public WarehouseRepository Warehouses { get; }

    public InventoryRepository Inventory { get; }

    public StockReceiptRepository Receipts { get; }

    public ProductRepository Products { get; }

    public UserRepository Users { get; }

    public string RootDirectory { get; }

    public ApplicationDirectoriesService Directories { get; }

    public LocalFileAccessService Files { get; }

    public City City { get; private set; } = null!;

    public User Keeper { get; private set; } = null!;

    public User OtherKeeper { get; private set; } = null!;

    public User KeeperWithoutProductsRead { get; private set; } = null!;

    public User Manager { get; private set; } = null!;

    public User Admin { get; private set; } = null!;

    public Warehouse MainWarehouse { get; private set; } = null!;

    public Warehouse SecondWarehouse { get; private set; } = null!;

    public Supplier Supplier { get; private set; } = null!;

    public Product LinkedProduct { get; private set; } = null!;

    public Product SecondLinkedProduct { get; private set; } = null!;

    public Product UnlinkedProduct { get; private set; } = null!;

    public AuthorizationService AuthorizationFor(User user, bool isAdmin = false) =>
        new(new FixedCurrentUser(user, isAdmin), new UserPermissionReader(Factory));

    public StockReceiptAttachment SeedDraft(
        User uploader,
        Warehouse warehouse,
        string originalFileName = "invoice.pdf",
        string storedFileName = null,
        string contentType = "application/pdf",
        long sizeBytes = 4,
        string description = null,
        DateTime? createdAtUtc = null)
    {
        var attachment = StockReceiptAttachment.CreateDraft(
            warehouse.Id,
            storedFileName ?? Guid.NewGuid().ToString("N") + ".pdf",
            originalFileName,
            description,
            sizeBytes,
            contentType,
            uploader.Id);

        if (createdAtUtc is DateTime createdAt)
            attachment.UpdateCreationDate(createdAt);

        Context.StockReceiptAttachments.Add(attachment);
        Context.SaveChanges();
        Context.ChangeTracker.Clear();
        return attachment;
    }

    private void Seed()
    {
        City = new City("الرياض");
        Context.Cities.Add(City);

        var keeperRole = new Role("أمين مخزن");
        keeperRole.SetPermission(PermissionFeature.Inventory, PermissionLevel.Write);
        keeperRole.SetPermission(PermissionFeature.Products, PermissionLevel.Read);
        keeperRole.SetPermission(PermissionFeature.Suppliers, PermissionLevel.Read);
        keeperRole.SetPermission(PermissionFeature.Warehouses, PermissionLevel.Read);

        var limitedKeeperRole = new Role("مورّد بلا قراءة منتجات");
        limitedKeeperRole.SetPermission(PermissionFeature.Inventory, PermissionLevel.Write);

        var managerRole = new Role("مدير");
        managerRole.SetPermission(PermissionFeature.Inventory, PermissionLevel.Approve);
        managerRole.SetPermission(PermissionFeature.Products, PermissionLevel.Read);
        managerRole.SetPermission(PermissionFeature.Suppliers, PermissionLevel.Read);

        Context.Roles.AddRange(keeperRole, limitedKeeperRole, managerRole);

        Keeper = User.Create("keeper1", "hash", "أمين المخزن");
        Keeper.AssignRole(keeperRole);

        OtherKeeper = User.Create("keeper2", "hash", "أمين المخزن الثاني");
        OtherKeeper.AssignRole(keeperRole);

        KeeperWithoutProductsRead = User.Create("keeper3", "hash", "أمين بلا منتجات");
        KeeperWithoutProductsRead.AssignRole(limitedKeeperRole);

        Manager = User.Create("manager", "hash", "مدير المخزون");
        Manager.AssignRole(managerRole);

        Admin = User.Create("admin", "hash", "المدير الأعلى", isAdmin: true);
        Admin.AssignRole(managerRole);

        Context.Users.AddRange(Keeper, OtherKeeper, KeeperWithoutProductsRead, Manager, Admin);
        Context.SaveChanges();

        MainWarehouse = new Warehouse("المخزن الرئيسي", Keeper.Id, City, "شارع الملك فهد");
        SecondWarehouse = new Warehouse("المخزن الثاني", OtherKeeper.Id, City, "طريق العروبة");
        Context.Warehouses.AddRange(MainWarehouse, SecondWarehouse);

        Supplier = new Supplier("مورد الجملة");
        Context.Suppliers.Add(Supplier);

        var category = new Category("مستلزمات");
        Context.Categories.Add(category);

        LinkedProduct = new Product("قفاز طبي", "GLV-001", category, new Money(10m));
        LinkedProduct.AddSupplier(Supplier);

        SecondLinkedProduct = new Product("كمامة طبية", "MSK-001", category, new Money(5m));
        SecondLinkedProduct.AddSupplier(Supplier);

        UnlinkedProduct = new Product("شاش طبي", "GAU-001", category, new Money(2m));

        Context.Products.AddRange(LinkedProduct, SecondLinkedProduct, UnlinkedProduct);

        Context.SaveChanges();
        Context.ChangeTracker.Clear();
    }

    public void Dispose()
    {
        Context.Dispose();

        try
        {
            if (Directory.Exists(RootDirectory))
                Directory.Delete(RootDirectory, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}

internal sealed class FixedCurrentUser(User user, bool isAdmin = false) : ICurrentUser
{
    public int? Id { get; } = user.Id;

    public string? UserName { get; } = user.UserName;

    public string? DisplayName { get; } = user.DisplayName;

    public bool IsAuthenticated { get; } = user.Id > 0;

    public bool IsAdmin { get; } = isAdmin;
}
