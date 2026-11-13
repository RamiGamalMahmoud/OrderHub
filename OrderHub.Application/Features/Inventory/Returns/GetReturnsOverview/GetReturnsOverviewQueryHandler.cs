using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Inventory.Returns.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.Returns.GetReturnsOverview;

internal sealed class GetReturnsOverviewQueryHandler(
    IWarehouseRepository warehouses,
    IInventoryReturnRepository returns,
    ICurrentUser currentUser) : IRequestHandler<GetReturnsOverviewQuery, Result<ReturnsOverview>>
{
    private const int HistoryTake = 20;
    private const int ReturnableOrdersTake = 50;

    public async Task<Result<ReturnsOverview>> Handle(
        GetReturnsOverviewQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result<ReturnsOverview>.Failure("يجب تسجيل الدخول لعرض المرتجعات.", "Unauthorized");

        Warehouse warehouse = await warehouses.GetByKeeperAsync(userId, cancellationToken);
        if (warehouse is null)
        {
            return Result<ReturnsOverview>.Failure(
                "صفحة المرتجعات متاحة لأمين المخزن المرتبط بمخزن فقط.",
                "Forbidden");
        }

        IReadOnlyList<PendingReturnRow> pending = await returns.GetPendingAsync(warehouse.Id, cancellationToken);
        IReadOnlyList<ReturnableOrderRow> returnable = await returns.GetReturnableOrdersAsync(
            warehouse.Id,
            ReturnableOrdersTake,
            cancellationToken);
        IReadOnlyList<ReturnHistoryRow> history = await returns.GetHistoryAsync(
            warehouse.Id,
            HistoryTake,
            cancellationToken);
        IReadOnlyList<DamagedProductRow> damaged = await returns.GetDamagedProductsAsync(
            warehouse.Id,
            cancellationToken);

        return Result<ReturnsOverview>.Success(new ReturnsOverview(
            warehouse.Id,
            warehouse.Name.Value,
            pending,
            returnable,
            history,
            damaged));
    }
}
