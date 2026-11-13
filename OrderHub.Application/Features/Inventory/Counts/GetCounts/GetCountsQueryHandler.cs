using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Inventory.Counts.Contracts;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.Counts.GetCounts;

internal sealed class GetCountsQueryHandler(
    IWarehouseRepository warehouses,
    IInventoryCountRepository counts,
    IAuthorizationService authorization,
    ICurrentUser currentUser) : IRequestHandler<GetCountsQuery, Result<CountOverview>>
{
    private const int Take = 100;

    public async Task<Result<CountOverview>> Handle(
        GetCountsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result<CountOverview>.Failure("يجب تسجيل الدخول لعرض الجرد.", "Unauthorized");

        bool canApprove = await authorization.IsAllowedAsync(
            PermissionFeature.Inventory,
            PermissionLevel.Approve,
            cancellationToken);

        Warehouse keeperWarehouse = await warehouses.GetByKeeperAsync(userId, cancellationToken);
        if (keeperWarehouse is not null)
        {
            IReadOnlyList<CountRow> rows = await counts.GetRowsAsync(
                keeperWarehouse.Id,
                Take,
                cancellationToken);
            IReadOnlyList<CountableProductRow> products = await counts.GetCountableProductsAsync(
                keeperWarehouse.Id,
                cancellationToken);
            return Result<CountOverview>.Success(new CountOverview(
                true,
                keeperWarehouse.Id,
                keeperWarehouse.Name.Value,
                rows,
                products));
        }

        if (canApprove)
        {
            IReadOnlyList<CountRow> rows = await counts.GetRowsAsync(null, Take, cancellationToken);
            return Result<CountOverview>.Success(new CountOverview(
                false,
                null,
                "كل المخازن",
                rows,
                []));
        }

        return Result<CountOverview>.Failure(
            "شاشة الجرد متاحة لأمين المخزن أو المدير المختص.",
            "Forbidden");
    }
}
