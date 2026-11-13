using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Presentation;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.ConsolidatedBalances;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Read)]
public sealed record GetConsolidatedInventoryBalancesQuery(
    string Search = null,
    int? CategoryId = null,
    StockStatusFilter Status = StockStatusFilter.All,
    bool HideZero = false,
    StockSortColumn Sort = StockSortColumn.Available,
    bool Descending = true,
    int Page = 1,
    int PageSize = 25) : IRequest<Result<ConsolidatedBalancesResult>>;
