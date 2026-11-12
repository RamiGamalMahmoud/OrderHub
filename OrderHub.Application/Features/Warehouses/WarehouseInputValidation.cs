using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Warehouses;

internal static class WarehouseInputValidation
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 100;

    public static bool TryNormalizeName(string name, out string normalized)
    {
        normalized = name?.Trim() ?? string.Empty;

        return normalized.Length is >= NameMinLength and <= NameMaxLength;
    }

    public static async Task<(User Keeper, string Error)> ResolveKeeperAsync(
        IUserRepository users,
        IWarehouseRepository warehouses,
        int keeperUserId,
        int? excludingWarehouseId,
        CancellationToken cancellationToken)
    {
        User keeper = await users.GetByIdAsync(keeperUserId, cancellationToken);

        if (keeper is null)
            return (null, "أمين المخزن غير موجود.");

        if (!keeper.IsActive)
            return (null, "لا يمكن اختيار مستخدم غير نشط كأمين مخزن.");

        if (await warehouses.IsKeeperOfAnotherWarehouseAsync(
                keeperUserId,
                excludingWarehouseId,
                cancellationToken))
            return (null, "هذا المستخدم أمين مخزن لمخزن آخر بالفعل.");

        return (keeper, null);
    }
}
