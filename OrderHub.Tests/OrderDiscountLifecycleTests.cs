using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.Services.Pricing;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Features.OrderDrafts;
using System.Text.Json;

namespace OrderHub.Tests;

public sealed class OrderDiscountLifecycleTests
{
    [Fact]
    public void Item_and_discount_changes_refresh_discount_and_keep_vat_separate()
    {
        var order = new Order(5, "ORD-CHANGE");
        Assert.True(order.AddOrderItem(1, "A", 400m, 1, 5m, "", null, []).IsSuccess);
        Assert.True(order.SetFinancialAdjustments(15m, DiscountType.Percentage, 10m).IsSuccess);
        Assert.Equal(40m, order.DiscountAmount);
        Assert.Equal(395m, order.Total.Value);

        Assert.True(order.AddOrderItem(2, "B", 100m, 1, 0m, "", null, []).IsSuccess);
        Assert.Equal(50m, order.DiscountAmount);
        Assert.Equal(485m, order.Total.Value);

        order.RemoveOrderItem(order.OrderItems.Single(item => item.ProductId == 2));
        Assert.Equal(40m, order.DiscountAmount);
        var item = order.OrderItems.Single();
        Assert.True(item.UpdateQuantity(2).IsSuccess);
        Assert.Equal(80m, order.DiscountAmount);
        Assert.True(item.ChangeUnitPrice(500m).IsSuccess);
        Assert.Equal(100m, order.DiscountAmount);
        Assert.True(item.ChangeVatRate(20m).IsSuccess);
        Assert.Equal(100m, order.DiscountAmount);
        Assert.Equal(1115m, order.Total.Value);

        Assert.True(order.SetFinancialAdjustments(25m, DiscountType.FixedAmount, 50m).IsSuccess);
        Assert.Equal(50m, order.DiscountAmount);
        Assert.Equal(1175m, order.Total.Value);
        order.ClearOrderItems();
        Assert.Equal(0m, order.DiscountAmount);
    }

    [Theory]
    [InlineData(DiscountType.FixedAmount, 30, 405)]
    [InlineData(DiscountType.Percentage, 10, 395)]
    public async Task Draft_persists_restores_financial_inputs_and_recalculates_after_product_changes(
        DiscountType type, decimal value, decimal expectedTotal)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await DisableForeignKeys(connection);
        await using var context = new AppDbContext(CreateOptions(connection));
        await context.Database.EnsureCreatedAsync();

        var draftId = Guid.NewGuid();
        var data = new OrderDraftData(
            5, "Client", OrderSource.Online, null, DeliveryMethod.DeliveryMan, 1, null,
            [new OrderDraftItem(1, "Product", "Category", 400m, 1, 5m, null, "", [])],
            [], 15m, type, value);
        var draft = new Draft(draftId, 7, DateTime.UtcNow, data);
        await new DraftRepository(context).StageSaveAsync(draft);
        await context.SaveChangesAsync();

        var readService = new DraftReadService(new AppDbContextFactory(CreateOptions(connection)));
        var restored = await readService.GetOwnedAsync(draftId, 7);

        Assert.NotNull(restored);
        Assert.Equal(type, restored.Data.DiscountType);
        Assert.Equal(value, restored.Data.DiscountValue);
        Assert.Equal(15m, restored.Data.DeliveryCost);

        var order = new Order(5, "ORD-DRAFT");
        var restoredItem = Assert.Single(restored.Data.Items);
        Assert.True(order.AddOrderItem(restoredItem.ProductId, restoredItem.ProductName, restoredItem.Price,
            restoredItem.Quantity, restoredItem.VAT, restoredItem.SupplierName, restoredItem.SupplierId, []).IsSuccess);
        Assert.True(order.SetFinancialAdjustments(restored.Data.DeliveryCost,
            restored.Data.DiscountType, restored.Data.DiscountValue).IsSuccess);
        Assert.Equal(type == DiscountType.Percentage ? 40m : 30m, order.DiscountAmount);
        Assert.Equal(expectedTotal, order.Total.Value);

        Assert.True(order.OrderItems.Single().UpdateQuantity(2).IsSuccess);
        Assert.Equal(type == DiscountType.Percentage ? 80m : 30m, order.DiscountAmount);
        Assert.Equal(type == DiscountType.Percentage ? 775m : 825m, order.Total.Value);
    }

    private static DbContextOptions<AppDbContext> CreateOptions(SqliteConnection connection)
        => new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).UseSnakeCaseNamingConvention().Options;

    private static async Task DisableForeignKeys(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = OFF";
        await command.ExecuteNonQueryAsync();
    }
}
