using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.Returns.RequestDisposal;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Write)]
public sealed record RequestDisposalCommand(
    int WarehouseId,
    int ProductId,
    int Quantity,
    string Reason) : IRequest<Result>;
