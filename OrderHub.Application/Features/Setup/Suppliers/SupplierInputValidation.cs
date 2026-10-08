using OrderHub.Domain.Common;
using OrderHub.Domain.ValueObjects;
using System;

namespace OrderHub.Application.Features.Setup.Suppliers;

internal static class SupplierInputValidation
{
    public static Result Validate(
        string name,
        string street,
        string number,
        string countryCode,
        TimeOnly openAt,
        TimeOnly closeAt)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2 || name.Trim().Length > 100)
        {
            return Result.Failure("اسم المورد يجب أن يتكون من 2 إلى 100 حرف.");
        }

        if (string.IsNullOrWhiteSpace(street) || street.Trim().Length > 150)
        {
            return Result.Failure("عنوان المورد مطلوب ويجب ألا يتجاوز 150 حرفًا.");
        }

        if (countryCode?.Trim().Length > 4 ||
            !PhoneNumber.CreatePhoneNumber(number?.Trim() ?? "", countryCode?.Trim() ?? "").IsSuccess)
        {
            return Result.Failure("أدخل رقم هاتف صحيحًا من 6 إلى 14 رقمًا ومفتاح دولة مثل +20 أو +966.");
        }

        if (openAt > closeAt)
        {
            return Result.Failure("وقت الإغلاق يجب ألا يسبق وقت الافتتاح.");
        }

        return Result.Success();
    }
}
