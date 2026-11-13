using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Approvals.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Inventory.Approvals.GetApprovals;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Approve)]
public sealed record GetApprovalsQuery(
    InventoryApprovalStatus? Status = null,
    InventoryApprovalRequestType? RequestType = null) : IRequest<Result<IReadOnlyList<InventoryApprovalProjectionRow>>>;
