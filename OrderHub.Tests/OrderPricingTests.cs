using OrderHub.Domain.Models;
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
}
