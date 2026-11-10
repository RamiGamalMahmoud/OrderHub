#nullable enable

using System.Linq;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Trips.Contracts;

public static class TripInputValidation
{
    public static Result ValidateCreate(
        string? title,
        string? description,
        TripPartyInput from,
        TripPartyInput to)
    {
        var result = ValidateCore(title, description);
        if (!result.IsSuccess)
        {
            return result;
        }

        result = ValidateParty(from, "الطرف المرسل");
        if (!result.IsSuccess)
        {
            return result;
        }

        return ValidateParty(to, "الطرف المستقبل");
    }

    public static Result ValidateUpdate(
        string? title,
        string? description,
        TripPartyInput from,
        TripPartyInput to,
        int currentDeliverymanId,
        int targetDeliverymanId,
        string? editNote,
        string? reassignReason)
    {
        var result = ValidateCreate(title, description, from, to);
        if (!result.IsSuccess)
        {
            return result;
        }

        if (editNote is not null && editNote.Trim().Length > 200)
        {
            return Result.Failure("ملاحظة التعديل يجب ألا تتجاوز 200 حرف.");
        }

        if (targetDeliverymanId != currentDeliverymanId)
        {
            var trimmedReason = reassignReason?.Trim();
            if (string.IsNullOrEmpty(trimmedReason) || trimmedReason.Length > 200)
            {
                return Result.Failure("سبب إعادة الإسناد مطلوب ويجب ألا يتجاوز 200 حرف.");
            }
        }

        return Result.Success();
    }

    public static Result ValidateParty(TripPartyInput? party, string label)
    {
        if (party is null)
        {
            return Result.Failure($"{label} مطلوب.");
        }

        if (!System.Enum.IsDefined(party.PartyType))
        {
            return Result.Failure($"{label}: نوع الطرف غير صالح.");
        }

        if (party.PartyType == TripPartyType.External)
        {
            var name = party.Name?.Trim();
            if (string.IsNullOrEmpty(name) || name.Length > 100)
            {
                return Result.Failure($"{label}: الاسم مطلوب ويجب ألا يتجاوز 100 حرف.");
            }

            var phone = party.Phone?.Trim();
            if (string.IsNullOrEmpty(phone))
            {
                return Result.Failure($"{label}: رقم الهاتف مطلوب.");
            }

            if (phone.Length < 6 || phone.Length > 22)
            {
                return Result.Failure($"{label}: رقم الهاتف يجب أن يكون بين 6 و 22 حرفاً.");
            }

            if (phone.Count(char.IsDigit) < 6)
            {
                return Result.Failure($"{label}: رقم الهاتف يجب أن يحتوي على 6 أرقام على الأقل.");
            }

            var address = party.Address?.Trim();
            if (string.IsNullOrEmpty(address) || address.Length > 255)
            {
                return Result.Failure($"{label}: العنوان مطلوب ويجب ألا يتجاوز 255 حرفاً.");
            }

            if (party.Location?.Trim().Length > 255)
            {
                return Result.Failure($"{label}: الموقع يجب ألا يتجاوز 255 حرفاً.");
            }

            return Result.Success();
        }

        if (party.PartyId is not > 0)
        {
            return Result.Failure($"{label}: يجب اختيار سجل محفوظ.");
        }

        return Result.Success();
    }

    private static Result ValidateCore(string? title, string? description)
    {
        var trimmedTitle = title?.Trim();
        if (string.IsNullOrEmpty(trimmedTitle) || trimmedTitle.Length > 150)
        {
            return Result.Failure("العنوان مطلوب ويجب ألا يتجاوز 150 حرفاً.");
        }

        if (description?.Trim().Length > 1000)
        {
            return Result.Failure("الوصف يجب ألا يتجاوز 1000 حرف.");
        }

        return Result.Success();
    }
}
