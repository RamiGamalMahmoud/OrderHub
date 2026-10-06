using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Setup.Deliverymen;

internal static class DeliverymanInputValidation
{
    public static Result Validate(string name, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2 || name.Trim().Length > 100)
        {
            return Result.Failure("اسم المندوب يجب أن يتكون من 2 إلى 100 حرف.");
        }

        if (phoneNumber?.Trim().Length > 15)
        {
            return Result.Failure("رقم هاتف المندوب يجب ألا يتجاوز 15 حرفًا.");
        }

        return Result.Success();
    }
}
