using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Inventory.GetStockAvailability;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Read)]
public sealed record GetStockAvailabilityQuery(int ProductId) : IRequest<IReadOnlyList<StockAvailabilityRow>>;

public sealed record StockAvailabilityRow(
    int WarehouseId,
    string WarehouseName,
    int StockOnHand,
    int ReservedOnHand,
    int Available);
