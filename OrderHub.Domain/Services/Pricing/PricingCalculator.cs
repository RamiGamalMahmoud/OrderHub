using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderHub.Domain.Services.Pricing;

public static class PricingCalculator
{
    public static PricingResult Calculate(IReadOnlyCollection<PricingItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
            throw new ArgumentException("At least one item is required.", nameof(items));

        var calculatedItems = items
            .Select(CalculateItem)
            .ToList();

        decimal subtotal = calculatedItems.Sum(x => x.Subtotal);
        decimal totalVat = calculatedItems.Sum(x => x.VatAmount);
        decimal totalAmount = calculatedItems.Sum(x => x.Total);

        return new PricingResult(
            calculatedItems,
            subtotal,
            totalVat,
            totalAmount);
    }

    public static PricingItemResult CalculateItem(PricingItem item)
    {
        if (item.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        if (item.UnitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative.");

        if (item.VatRate < 0)
            throw new ArgumentException("VAT rate cannot be negative.");

        decimal subtotal = item.Quantity * item.UnitPrice;

        decimal vatAmount = subtotal * item.VatRate / 100m;

        decimal total = subtotal + vatAmount;

        return new PricingItemResult(
            item.ProductId,
            item.ProductName,
            item.Quantity,
            item.UnitPrice,
            item.VatRate,
            subtotal,
            vatAmount,
            total);
    }
}