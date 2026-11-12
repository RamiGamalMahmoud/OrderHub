using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.GetStockInContext;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Write)]
public sealed record GetStockInContextQuery : IRequest<Result<StockInContext>>;

public sealed record StockInContext(
    int WarehouseId,
    string WarehouseName,
    bool IsOpeningAllowed,
    bool CanReadProducts,
    bool CanSelectSupplier);
