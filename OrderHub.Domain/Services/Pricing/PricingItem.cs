namespace OrderHub.Domain.Services.Pricing;

public sealed record PricingItem(
    int ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal VatRate);