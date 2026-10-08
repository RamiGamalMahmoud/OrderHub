using OrderHub.Application.Features.Setup.ShippingCarriers;
using OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;

namespace OrderHub.Web.Components.Pages.Features.ShippingCarriers.Editor;

public sealed class ShippingCarrierEditorState
{
    public string Name { get; set; } = string.Empty;

    public decimal ShippingCost { get; set; }

    public int? CityId { get; set; }

    public string Street { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string CountryCode { get; set; } = string.Empty;

    public static ShippingCarrierEditorState FromRead(ShippingCarrierEdit carrier) => new()
    {
        Name = carrier.Name,
        ShippingCost = carrier.ShippingCost,
        CityId = carrier.CityId > 0 ? carrier.CityId : null,
        Street = carrier.Street,
        PhoneNumber = carrier.PhoneNumber,
        CountryCode = carrier.CountryCode
    };

    public CreateShippingCarrierCommand CreateCommand() => new(new(
        Name,
        ShippingCost,
        CountryCode,
        PhoneNumber,
        CityId.GetValueOrDefault(),
        Street));

    public UpdateShippingCarrierCommand UpdateCommand(int carrierId) => new(new(
        carrierId,
        Name,
        ShippingCost,
        CountryCode,
        PhoneNumber,
        CityId.GetValueOrDefault(),
        Street));

    public static string? ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2 || name.Trim().Length > 100)
        {
            return "اسم شركة الشحن يجب أن يتكون من 2 إلى 100 حرف.";
        }

        return null;
    }

    public string? ValidatePhone(string value) =>
        OrderHub.Domain.ValueObjects.PhoneNumber.CreatePhoneNumber(
            value?.Trim() ?? "",
            CountryCode?.Trim() ?? "").IsSuccess
                ? null
                : "أدخل رقم هاتف صحيحًا من 6 إلى 14 رقمًا.";

    public string? ValidateCountryCode(string value) =>
        value?.Trim().Length <= 4 &&
        OrderHub.Domain.ValueObjects.PhoneNumber.CreatePhoneNumber("123456", value?.Trim() ?? "").IsSuccess
            ? null
            : "أدخل مفتاح دولة صحيحًا مثل +20 أو +966.";

    public string? ValidateAdditional() =>
        ShippingCost < 0 || ShippingCost > 9999999999999999.99m
            ? "تكلفة الشحن يجب أن تكون غير سالبة ضمن النطاق المسموح." : null;
}
