namespace OrderHub.Domain.Services.Pricing;

public sealed record DocumentItemCalculation(
    int ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal VatRate,
    decimal VatAmount,
    decimal LineTotal);