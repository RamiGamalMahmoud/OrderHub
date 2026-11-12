using OrderHub.Application.Features.Warehouses.Contracts;
using OrderHub.Application.Features.Warehouses.GetKeeperCandidates;
using OrderHub.Domain.ValueObjects;
using System.Collections.Generic;

namespace OrderHub.Web.Components.Pages.Features.Warehouses.Editor;

public sealed class WarehouseEditorState
{
    public string Name { get; set; } = string.Empty;

    public int? KeeperUserId { get; set; }

    public int? CityId { get; set; }

    public string Street { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string CountryCode { get; set; } = "+966";

    public bool IsActive { get; set; } = true;

    public string? PreviousKeeperDisplayName { get; set; }

    public List<WarehouseKeeperCandidate> Candidates { get; } = [];

    public static WarehouseEditorState FromWarehouse(WarehouseEdit warehouse) => new()
    {
        Name = warehouse.Name,
        KeeperUserId = warehouse.KeeperUserId,
        CityId = warehouse.CityId,
        Street = warehouse.Street,
        PhoneNumber = warehouse.PhoneNumber,
        CountryCode = string.IsNullOrWhiteSpace(warehouse.CountryCode) ? "+966" : warehouse.CountryCode,
        IsActive = warehouse.IsActive
    };

    public static string? ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2 || name.Trim().Length > 100)
        {
            return "اسم المخزن يجب أن يتكون من 2 إلى 100 حرف.";
        }

        return null;
    }

    public static string? ValidateCity(int? cityId) =>
        cityId is > 0 ? null : "اختر المدينة.";

    public static string? ValidateStreet(string street)
    {
        int length = street?.Trim().Length ?? 0;

        return length is >= 1 and <= 150
            ? null
            : "عنوان المخزن مطلوب ويجب ألا يتجاوز 150 حرفًا.";
    }

    public string? ValidatePhone(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        return OrderHub.Domain.ValueObjects.PhoneNumber.CreatePhoneNumber(
            phoneNumber.Trim(),
            CountryCode?.Trim() ?? string.Empty).IsSuccess
            ? null
            : "أدخل رقم هاتف صحيحًا من 6 إلى 14 رقمًا.";
    }

    public string? ValidateCountryCode(string countryCode) =>
        string.IsNullOrWhiteSpace(PhoneNumber)
            ? null
            : countryCode?.Trim().Length <= 4 &&
              OrderHub.Domain.ValueObjects.PhoneNumber.CreatePhoneNumber("123456", countryCode?.Trim() ?? string.Empty).IsSuccess
                ? null
                : "أدخل مفتاح دولة صحيحًا مثل +20 أو +966.";
}
