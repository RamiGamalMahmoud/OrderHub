using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Domain.Services.Pricing;

public sealed record OrderFinancialResult(
    decimal ProductsSubtotalBeforeVat,
    decimal ProductsVatAmount,
    decimal ProductsTotalIncludingVat,
    decimal DiscountAmount,
    decimal DeliveryCost,
    decimal FinalTotal);

public static class OrderFinancialCalculator
{
    public static Result<OrderFinancialResult> Calculate(
        PricingResult pricing,
        decimal deliveryCost,
        DiscountType discountType,
        decimal discountValue)
    {
        if (deliveryCost < 0) return Result<OrderFinancialResult>.Failure("تكلفة التوصيل لا يمكن أن تكون سالبة.");
        if (!System.Enum.IsDefined(discountType)) return Result<OrderFinancialResult>.Failure("نوع الخصم غير صالح.");
        if (discountValue < 0) return Result<OrderFinancialResult>.Failure("قيمة الخصم لا يمكن أن تكون سالبة.");
        if (pricing.Items.Count == 0 && discountValue > 0) return Result<OrderFinancialResult>.Failure("لا يمكن تطبيق خصم دون منتجات.");
        if (discountType == DiscountType.Percentage && discountValue > 100) return Result<OrderFinancialResult>.Failure("نسبة الخصم يجب أن تكون بين 0 و100.");
        if (discountType == DiscountType.FixedAmount && discountValue > pricing.Subtotal) return Result<OrderFinancialResult>.Failure("الخصم لا يمكن أن يتجاوز إجمالي المنتجات قبل الضريبة.");

        var discountAmount = discountType == DiscountType.Percentage
            ? pricing.Subtotal * discountValue / 100m
            : discountValue;
        var finalTotal = CalculateFinalTotal(pricing.TotalAmount, discountAmount, deliveryCost);
        if (discountAmount < 0 || discountAmount > pricing.Subtotal || finalTotal < 0)
            return Result<OrderFinancialResult>.Failure("قيمة الخصم تؤدي إلى إجمالي غير صالح.");

        return Result<OrderFinancialResult>.Success(new OrderFinancialResult(
            pricing.Subtotal, pricing.TotalVat, pricing.TotalAmount, discountAmount, deliveryCost, finalTotal));
    }

    public static decimal CalculateFinalTotal(decimal productsTotalIncludingVat, decimal discountAmount, decimal deliveryCost)
        => productsTotalIncludingVat - discountAmount + deliveryCost;
}
