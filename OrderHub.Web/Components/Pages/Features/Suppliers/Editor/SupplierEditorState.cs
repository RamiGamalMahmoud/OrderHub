using OrderHub.Application.Features.Setup.Suppliers;
using OrderHub.Application.Features.Setup.Suppliers.Contracts;
using OrderHub.Domain.ValueObjects;

namespace OrderHub.Web.Components.Pages.Features.Suppliers.Editor;

public sealed class SupplierEditorState
{
    public string Name { get; set; } = string.Empty;

    public int? CityId { get; set; }

    public string Street { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string CountryCode { get; set; } = string.Empty;

    public TimeSpan? OpenAt { get; set; }

    public TimeSpan? CloseAt { get; set; }

    public static SupplierEditorState FromRead(SupplierEdit supplier) => new()
    {
        Name = supplier.Name,
        CityId = supplier.CityId > 0 ? supplier.CityId : null,
        Street = supplier.Street,
        PhoneNumber = supplier.PhoneNumber,
        CountryCode = supplier.CountryCode,
        OpenAt = supplier.OpenAt?.ToTimeSpan(),
        CloseAt = supplier.CloseAt?.ToTimeSpan()
    };

    public CreateSupplierCommand CreateCommand() => new(new(
        Name,
        TimeOnly.FromTimeSpan(OpenAt!.Value),
        TimeOnly.FromTimeSpan(CloseAt!.Value),
        Street,
        CityId.GetValueOrDefault(),
        PhoneNumber,
        CountryCode));

    public UpdateSupplierCommand UpdateCommand(int supplierId) => new(new(
        supplierId,
        Name,
        TimeOnly.FromTimeSpan(OpenAt!.Value),
        TimeOnly.FromTimeSpan(CloseAt!.Value),
        Street,
        CityId.GetValueOrDefault(),
        PhoneNumber,
        CountryCode));

    public static string? ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2 || name.Trim().Length > 100)
        {
            return "اسم المورد يجب أن يتكون من 2 إلى 100 حرف.";
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

    public string? ValidateAdditional()
    {
        if (OpenAt is null || CloseAt is null)
        {
            return "أدخل وقت الافتتاح والإغلاق.";
        }

        if (OpenAt > CloseAt)
        {
            return "وقت الإغلاق يجب ألا يسبق وقت الافتتاح.";
        }

        return null;
    }
}
