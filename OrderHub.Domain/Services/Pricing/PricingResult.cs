using System.Collections.Generic;

namespace OrderHub.Domain.Services.Pricing;

public sealed record PricingResult(
    IReadOnlyCollection<PricingItemResult> Items,
    decimal Subtotal,
    decimal TotalVat,
    decimal TotalAmount);