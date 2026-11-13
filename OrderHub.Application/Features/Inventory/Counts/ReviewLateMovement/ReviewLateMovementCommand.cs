using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Counts.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.Counts.ReviewLateMovement;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Approve)]
public sealed record ReviewLateMovementCommand(
    int CountId,
    int LineId,
    LateMovementDecision Decision,
    string Note = null) : IRequest<Result<CountDecisionResult>>;
