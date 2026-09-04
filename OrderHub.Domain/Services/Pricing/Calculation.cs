using System.Collections.Generic;

namespace OrderHub.Domain.Services.Pricing;

public sealed record Calculation(
    decimal Subtotal,
    decimal TotalVat,
    decimal TotalAmount,
    IReadOnlyCollection<ItemCalculation> Items);