using OrderHub.Domain.Common;
using OrderHub.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderHub.Application.Features.Setup.Suppliers;

internal static class SupplierInputValidation
{
    public static Result Validate(
        string name,
        string street,
        string number,
        string countryCode,
        TimeOnly? openAt,
        TimeOnly? closeAt,
        IEnumerable<int> productIds)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2 || name.Trim().Length > 100)
        {
            return Result.Failure("اسم المورد يجب أن يتكون من 2 إلى 100 حرف.");
        }

        var ids = productIds?.ToArray() ?? [];

        if (ids.Any(id => id <= 0) || ids.Distinct().Count() != ids.Length)
        {
            return Result.Failure("اختيار المنتجات غير صالح أو يحتوي على تكرار.");
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

        // ساعات العمل اختيارية بالكامل؛ فقط يمنع تقديم وقت الإغلاق على وقت الافتتاح.
        if (openAt > closeAt)
        {
            return Result.Failure("وقت الإغلاق يجب ألا يسبق وقت الافتتاح.");
        }

        return Result.Success();
    }
}
