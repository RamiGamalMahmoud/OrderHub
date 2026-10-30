using OrderHub.Domain.Models;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Services.Pricing;

namespace OrderHub.Tests;

public sealed class OrderPricingTests
{
    [Fact]
    public void Order_total_includes_each_item_vat_using_the_shared_pricing_calculator()
    {
        var order = new Order(5, "ORD-TAX");
        Assert.True(order.AddOrderItem(1, "منتج أ", 100m, 2, 15m, "", null, []).IsSuccess);
        Assert.True(order.AddOrderItem(2, "منتج ب", 50m, 1, 10m, "", null, []).IsSuccess);

        var pricing = order.Pricing;

        Assert.Equal(250m, pricing.Subtotal);
        Assert.Equal(35m, pricing.TotalVat);
        Assert.Equal(285m, pricing.TotalAmount);
        Assert.Equal(285m, order.Total.Value);
        Assert.Equal(new[] { 30m, 5m }, pricing.Items.Select(item => item.VatAmount));
    }

    [Fact]
    public void Pricing_calculator_adds_tax_to_the_pre_tax_unit_price_and_quantity_subtotal()
    {
        var result = PricingCalculator.CalculateItem(new PricingItem(1, "منتج", 3, 20m, 15m));

        Assert.Equal(60m, result.Subtotal);
        Assert.Equal(9m, result.VatAmount);
        Assert.Equal(69m, result.Total);
    }

    [Theory]
    [InlineData(0, 0, 0, 420)]
    [InlineData(1, 10, 15, 395)]
    [InlineData(0, 30, 15, 405)]
    [InlineData(1, 100, 15, 35)]
    [InlineData(0, 400, 0, 20)]
    public void Order_calculates_discount_against_pre_tax_subtotal_then_adds_delivery(int type, decimal value, decimal delivery, decimal expectedTotal)
    {
        var order = new Order(5, "ORD-DISCOUNT");
        Assert.True(order.AddOrderItem(1, "منتج", 400m, 1, 5m, "", null, []).IsSuccess);
        var result = order.SetFinancialAdjustments(delivery, (DiscountType)type, value);

        Assert.True(result.IsSuccess);
        Assert.Equal(type == 1 ? value * 4m : value, order.DiscountAmount);
        Assert.Equal(expectedTotal, order.Total.Value);
    }

    [Theory]
    [InlineData(-1, 0, 0, false)]
    [InlineData(0, 0, -1, false)]
    [InlineData(0, 1, 101, false)]
    [InlineData(0, 0, 401, true)]
    [InlineData(0, 0, 1, false)]
    public void Order_rejects_invalid_financial_adjustments(decimal delivery, int type, decimal discount, bool addProduct)
    {
        var order = new Order(5, "ORD-INVALID");
        if (addProduct)
            Assert.True(order.AddOrderItem(1, "منتج", 400m, 1, 5m, "", null, []).IsSuccess);

        var result = order.SetFinancialAdjustments(delivery, (DiscountType)type, discount);

        Assert.False(result.IsSuccess);
    }
}
