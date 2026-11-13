using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.WarehouseLedger;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Read)]
public sealed record GetWarehouseLedgerQuery(
    int WarehouseId,
    int ProductId,
    int Page = 1,
    int PageSize = 10) : IRequest<Result<WarehouseLedgerPage>>;
