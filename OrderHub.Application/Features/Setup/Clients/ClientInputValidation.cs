using OrderHub.Application.Features.Setup.Clients.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.ValueObjects;
using System;

namespace OrderHub.Application.Features.Setup.Clients;

internal static class ClientInputValidation
{
    public static Result Validate(ClientInput input)
    {
        if (input is null)
        {
            return Result.Failure("بيانات العميل مطلوبة.");
        }

        try
        {
            _ = new EntityName(input.Name?.Trim());
        }
        catch (ArgumentException)
        {
            return Result.Failure("اسم العميل يجب أن يتكون من 2 إلى 100 حرف.");
        }

        if (string.IsNullOrWhiteSpace(input.Street) || input.Street.Trim().Length > 150)
        {
            return Result.Failure("عنوان العميل مطلوب ويجب ألا يتجاوز 150 حرفًا.");
        }

        if (input.CityId <= 0)
        {
            return Result.Failure("اختر مدينة صحيحة.");
        }

        if (input.CountryCode?.Trim().Length > 4 ||
            !PhoneNumber.CreatePhoneNumber(input.PhoneNumber?.Trim() ?? "", input.CountryCode?.Trim() ?? "").IsSuccess)
        {
            return Result.Failure("أدخل رقم هاتف صحيحًا من 6 إلى 14 رقمًا ومفتاح دولة مثل +20 أو +966.");
        }

        if (input.Location?.Trim().Length > 255)
        {
            return Result.Failure("الموقع يجب ألا يتجاوز 255 حرفًا.");
        }

        return Result.Success();
    }
}
