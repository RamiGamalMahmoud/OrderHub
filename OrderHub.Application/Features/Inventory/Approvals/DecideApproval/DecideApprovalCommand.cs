using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.Approvals.DecideApproval;

/// <summary>
/// Single decision on a pending approval (D21); <paramref name="ExpectedVersion"/> is the
/// version the approver saw, so a concurrent decision or refresh is rejected as a conflict.
/// </summary>
[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Approve)]
public sealed record DecideApprovalCommand(
    int ApprovalId,
    bool Approve,
    string Note,
    int ExpectedVersion) : IRequest<Result>;
