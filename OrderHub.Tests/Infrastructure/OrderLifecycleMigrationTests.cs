using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using OrderHub.Infrastructure.Migrations;

namespace OrderHub.Tests.Infrastructure;

public class OrderLifecycleMigrationTests
{
    [LifecyclePostgresFact]
    public async Task Legacy_migration_preserves_business_data_and_initializes_every_status_without_duplicates()
    {
        using var fixture = new OrderLifecyclePostgresDatabase(createSchema: false);
        using var db = fixture.CreateDbContext();
        await db.GetService<IMigrator>().MigrateAsync("20261006094846_RestrictSupplierProductLinks");
        var city = new City("City");
        var client = new Client("Client", "Street", city, "01012345678", "+20");
        var courier = new Deliveryman("Courier", city, "01012345678");
        var product = new Product("Current product", "P1", new Category("Category"), new Money(99));
        db.AddRange(client, courier, product);
        await db.SaveChangesAsync();
        var at = new DateTime(2025, 3, 4, 5, 6, 7, DateTimeKind.Utc);
        foreach (var status in new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" })
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO orders (client_id, order_number, order_status, delivery_method, created_at)
                VALUES ({client.Id}, {status}, {status}, 'DeliveryChain', {at})
                """);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO order_items (product_id, order_id, product_name, unit_price, quantity, vat_rate, supplier_name, created_at)
                SELECT {product.Id}, id, 'Saved snapshot', 12.34, 3, 14, 'Saved supplier', {at} FROM orders WHERE order_number={status}
                """);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO order_delivery_steps (order_id, step_order, delivery_method, deliveryman_id, created_at)
                SELECT id, 10, 'DeliveryMan', {courier.Id}, {at.AddDays(1)} FROM orders WHERE order_number={status}
                """);
        }
        var beforeIds = await db.Database.SqlQueryRaw<int>("SELECT id AS \"Value\" FROM orders ORDER BY id").ToListAsync();
        var beforeStepIds = await db.Database.SqlQueryRaw<int>("SELECT id AS \"Value\" FROM order_delivery_steps ORDER BY id").ToListAsync();
        var invoice = OrderHub.Domain.Models.CommercialDocuments.Invoice.Create("INV-LEGACY", at,
            "Saved client", "Saved phone", "Saved address", 10, 0, 10, beforeIds[0]);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        await db.Database.MigrateAsync();
        var orders = await db.Orders.AsNoTracking().Include(order => order.OrderItems).Include(order => order.DeliverySteps)
            .Include(order => order.StatusHistory).OrderBy(order => order.Id).ToListAsync();
        Assert.Equal(beforeIds, orders.Select(order => order.Id));
        Assert.Equal(beforeStepIds, orders.SelectMany(order => order.DeliverySteps).Select(step => step.Id).OrderBy(id => id));
        var savedInvoice = await db.Invoices.AsNoTracking().SingleAsync();
        Assert.Equal(invoice.Id, savedInvoice.Id);
        Assert.Equal(beforeIds[0], savedInvoice.OrderId);
        Assert.Equal("Saved client", savedInvoice.CustomerName);
        Assert.All(orders, order =>
        {
            Assert.Equal(at, order.CreatedAt);
            var item = Assert.Single(order.OrderItems);
            Assert.Equal("Saved snapshot", item.ProductName);
            Assert.Equal("Saved supplier", item.SupplierName);
            Assert.Equal(12.34m, item.UnitPrice.Value);
            Assert.Equal(3, item.Quantity);
            Assert.Equal(14m, item.VatRate);
            var step = Assert.Single(order.DeliverySteps);
            Assert.Equal(at.AddDays(1), step.CreatedAt);
            Assert.Equal(10, step.StepOrder);
            Assert.Equal(courier.Id, step.DeliverymanId);
            var history = order.StatusHistory.OrderBy(record => record.ChangedAtUtc).ToList();
            Assert.Equal(OrderStatus.Processing, history[0].NewStatus);
            Assert.Equal(order.OrderNumber, history[0].OriginalStatus);
            Assert.Equal(at, history[0].ChangedAtUtc);
            Assert.All(history, record =>
            {
                Assert.Equal(OrderHistorySource.LegacyMigration, record.Source);
                Assert.Null(record.ChangedByUserId);
                Assert.Null(record.Reason);
            });
            if (order.OrderNumber is "Delivered" or "Cancelled")
            {
                Assert.Equal(2, history.Count);
                Assert.Equal(at.AddSeconds(1), history[1].ChangedAtUtc);
                Assert.Equal(order.OrderNumber, order.OrderStatus.ToString());
                Assert.Equal(order.OrderNumber == "Delivered" ? DeliveryStatus.Completed : DeliveryStatus.Cancelled, step.Status);
            }
            else
            {
                Assert.Single(history);
                Assert.Equal(OrderStatus.Processing, order.OrderStatus);
                Assert.Equal(DeliveryStatus.Pending, step.Status);
            }
        });
        Assert.Equal(7, await db.OrderStatusHistories.CountAsync());
        Assert.Equal(2, await db.DeliveryStepStatusHistories.CountAsync());
        await using (var repeat = await db.Database.BeginTransactionAsync())
        {
            await db.Database.ExecuteSqlRawAsync(LegacyOrderLifecycle.Sql);
            await repeat.CommitAsync();
        }
        Assert.Equal(7, await db.OrderStatusHistories.CountAsync());
        Assert.Equal(2, await db.DeliveryStepStatusHistories.CountAsync());
    }

    [LifecyclePostgresFact]
    public async Task Invalid_legacy_date_rejects_initialization_without_partial_history()
    {
        using var fixture = new OrderLifecyclePostgresDatabase();
        using var db = fixture.CreateDbContext();
        await OrderLifecyclePersistenceTests.Seed(db);
        await db.Database.ExecuteSqlRawAsync("UPDATE orders SET created_at = 'infinity'::timestamptz");
        await using var transaction = await db.Database.BeginTransactionAsync();
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => db.Database.ExecuteSqlRawAsync(LegacyOrderLifecycle.Sql));
        await transaction.RollbackAsync();
        Assert.Single(await db.OrderStatusHistories.ToListAsync());
        Assert.Empty(await db.DeliveryStepStatusHistories.ToListAsync());
    }
}
