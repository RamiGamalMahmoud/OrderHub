using OrderHub.Domain.Common;
using OrderHub.Domain.ValueObjects;

namespace OrderHub.Application.Features.Setup.ShippingCarriers;

internal static class ShippingCarrierInputValidation
{
    public static Result Validate(
        string name,
        decimal shippingCost,
        string street,
        string number,
        string countryCode)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2 || name.Trim().Length > 100)
        {
            return Result.Failure("اسم شركة الشحن يجب أن يتكون من 2 إلى 100 حرف.");
        }

        if (shippingCost < 0 || shippingCost > 9999999999999999.99m)
        {
            return Result.Failure("تكلفة الشحن يجب أن تكون قيمة غير سالبة ضمن النطاق المسموح.");
        }

        if (string.IsNullOrWhiteSpace(street) || street.Trim().Length > 150)
        {
            return Result.Failure("عنوان شركة الشحن مطلوب ويجب ألا يتجاوز 150 حرفًا.");
        }

        if (countryCode?.Trim().Length > 4 ||
            !PhoneNumber.CreatePhoneNumber(number?.Trim() ?? "", countryCode?.Trim() ?? "").IsSuccess)
        {
            return Result.Failure("أدخل رقم هاتف صحيحًا من 6 إلى 14 رقمًا ومفتاح دولة مثل +20 أو +966.");
        }

        return Result.Success();
    }
}
