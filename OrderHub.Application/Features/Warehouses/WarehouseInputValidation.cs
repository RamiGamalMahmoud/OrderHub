using OrderHub.Application.Features.Setup.Cities.Contracts;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Warehouses;

internal static class WarehouseInputValidation
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 100;
    public const int StreetMaxLength = 150;

    public static bool TryNormalizeName(string name, out string normalized)
    {
        normalized = name?.Trim() ?? string.Empty;

        return normalized.Length is >= NameMinLength and <= NameMaxLength;
    }

    public static async Task<(City City, string Error)> ResolveAddressAsync(
        ICityRepository cities,
        int cityId,
        string street,
        CancellationToken cancellationToken)
    {
        string normalizedStreet = street?.Trim() ?? string.Empty;

        if (normalizedStreet.Length is 0 or > StreetMaxLength)
            return (null, "عنوان المخزن مطلوب ويجب ألا يتجاوز 150 حرفًا.");

        if (cityId <= 0)
            return (null, "اختر المدينة.");

        City city = await cities.FindAsync(cityId, cancellationToken);

        if (city is null)
            return (null, "المدينة غير موجودة.");

        return (city, null);
    }

    /// <summary>A blank number means no phone; otherwise the number and country code must both be valid.</summary>
    public static bool TryNormalizePhone(
        string countryCode,
        string phoneNumber,
        out string normalizedCountryCode,
        out string normalizedPhoneNumber,
        out string error)
    {
        normalizedCountryCode = countryCode?.Trim() ?? string.Empty;
        normalizedPhoneNumber = phoneNumber?.Trim() ?? string.Empty;
        error = null;

        if (normalizedPhoneNumber.Length == 0)
        {
            normalizedCountryCode = string.Empty;
            return true;
        }

        if (!PhoneNumber.CreatePhoneNumber(normalizedPhoneNumber, normalizedCountryCode).IsSuccess)
        {
            error = "أدخل رقم هاتف صحيحًا من 6 إلى 14 رقمًا ومفتاح دولة مثل +20 أو +966.";
            return false;
        }

        return true;
    }

    public static async Task<(User Keeper, string Error)> ResolveKeeperAsync(
        IUserRepository users,
        IWarehouseRepository warehouses,
        int keeperUserId,
        int? excludingWarehouseId,
        CancellationToken cancellationToken)
    {
        User keeper = await users.GetWithRolePermissionsAsync(keeperUserId, cancellationToken);

        if (keeper is null)
            return (null, "أمين المخزن غير موجود.");

        if (!keeper.IsActive)
            return (null, "لا يمكن اختيار مستخدم غير نشط كأمين مخزن.");

        if (!CanKeepWarehouse(keeper))
            return (null, "لا يمكن اختيار مستخدم لا يملك صلاحية كتابة أو اعتماد المخزون كأمين مخزن.");

        if (await warehouses.IsKeeperOfAnotherWarehouseAsync(
                keeperUserId,
                excludingWarehouseId,
                cancellationToken))
            return (null, "هذا المستخدم أمين مخزن لمخزن آخر بالفعل.");

        return (keeper, null);
    }

    public static bool CanKeepWarehouse(User user) =>
        user.IsAdmin ||
        user.Role?.Permissions.Any(permission =>
            permission.Feature == PermissionFeature.Inventory &&
            (permission.Level == PermissionLevel.Write ||
             permission.Level == PermissionLevel.Approve)) == true;
}
