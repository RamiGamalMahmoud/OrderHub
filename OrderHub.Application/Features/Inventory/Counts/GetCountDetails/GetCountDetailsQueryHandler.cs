using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Inventory.Counts.Contracts;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.Counts.GetCountDetails;

internal sealed class GetCountDetailsQueryHandler(
    IWarehouseRepository warehouses,
    IInventoryCountRepository counts,
    IAuthorizationService authorization,
    ICurrentUser currentUser) : IRequestHandler<GetCountDetailsQuery, Result<CountDetails>>
{
    public async Task<Result<CountDetails>> Handle(
        GetCountDetailsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result<CountDetails>.Failure("يجب تسجيل الدخول لعرض الجرد.", "Unauthorized");

        CountDetails details = await counts.GetDetailsAsync(request.CountId, cancellationToken);
        if (details is null)
            return Result<CountDetails>.Failure("الجرد غير موجود.", "NotFound");

        Warehouse keeperWarehouse = await warehouses.GetByKeeperAsync(userId, cancellationToken);
        bool isKeeper = keeperWarehouse?.Id == details.WarehouseId;

        if (!isKeeper && !currentUser.IsAdmin)
        {
            bool canApprove = await authorization.IsAllowedAsync(
                PermissionFeature.Inventory,
                PermissionLevel.Approve,
                cancellationToken);
            if (!canApprove)
            {
                return Result<CountDetails>.Failure(
                    "عرض هذا الجرد متاح لأمين المخزن أو المدير المختص.",
                    "Forbidden");
            }
        }

        return Result<CountDetails>.Success(details with { IsKeeper = isKeeper });
    }
}
