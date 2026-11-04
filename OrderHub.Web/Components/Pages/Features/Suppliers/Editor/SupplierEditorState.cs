using OrderHub.Application.Features.Setup.Suppliers;
using OrderHub.Application.Features.Setup.Suppliers.Contracts;
using OrderHub.Domain.ValueObjects;

namespace OrderHub.Web.Components.Pages.Features.Suppliers.Editor;

public sealed class SupplierEditorState
{
    private readonly int? lockedProductId;

    public SupplierEditorState() : this(null)
    {
    }

    public SupplierEditorState(int? lockedProductId)
    {
        this.lockedProductId = lockedProductId;

        // المنتج المفتاح منه المحرر يبقى مرتبطًا دائمًا بالمورد.
        if (lockedProductId is int productId)
        {
            ProductIds.Add(productId);
        }
    }

    public string Name { get; set; } = string.Empty;

    public int? CityId { get; set; }

    public string Street { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string CountryCode { get; set; } = string.Empty;

    public HashSet<int> ProductIds { get; } = [];

    public TimeSpan? OpenAt { get; set; }

    public TimeSpan? CloseAt { get; set; }

    public static SupplierEditorState FromRead(SupplierEdit supplier, int? lockedProductId = null)
    {
        var state = new SupplierEditorState(lockedProductId)
        {
            Name = supplier.Name,
            CityId = supplier.CityId > 0 ? supplier.CityId : null,
            Street = supplier.Street,
            PhoneNumber = supplier.PhoneNumber,
            CountryCode = supplier.CountryCode,
            OpenAt = supplier.OpenAt?.ToTimeSpan(),
            CloseAt = supplier.CloseAt?.ToTimeSpan()
        };

        state.ProductIds.UnionWith(supplier.SelectedProductsIds);

        return state;
    }

    public bool IsProductLocked(int productId) => lockedProductId == productId;

    public bool SetProduct(int productId, bool selected)
    {
        if (lockedProductId == productId)
        {
            return false;
        }

        if (selected)
        {
            ProductIds.Add(productId);
        }
        else
        {
            ProductIds.Remove(productId);
        }

        return true;
    }

    public CreateSupplierCommand CreateCommand() => new(new(
        Name,
        ToNullableTimeOnly(OpenAt),
        ToNullableTimeOnly(CloseAt),
        Street,
        CityId.GetValueOrDefault(),
        PhoneNumber,
        CountryCode,
        ProductIds.ToArray()));

    public UpdateSupplierCommand UpdateCommand(int supplierId) => new(new(
        supplierId,
        Name,
        ToNullableTimeOnly(OpenAt),
        ToNullableTimeOnly(CloseAt),
        Street,
        CityId.GetValueOrDefault(),
        PhoneNumber,
        CountryCode,
        ProductIds.ToArray()));

    private static TimeOnly? ToNullableTimeOnly(TimeSpan? value) =>
        value is null ? null : TimeOnly.FromTimeSpan(value.Value);

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
        // ساعات العمل اختيارية بالكامل؛ فقط يمنع تقديم وقت الإغلاق على وقت الافتتاح.
        if (OpenAt is not null && OpenAt > CloseAt)
        {
            return "وقت الإغلاق يجب ألا يسبق وقت الافتتاح.";
        }

        return null;
    }
}
