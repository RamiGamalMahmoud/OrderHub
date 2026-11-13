using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.ConsolidatedBalances;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Read)]
public sealed record GetProductWarehouseDistributionQuery(int ProductId)
    : IRequest<Result<ProductWarehouseDistributionResult>>;
