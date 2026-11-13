using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.Approvals.RequestMovementReversal;

/// <summary>Keeper-initiated request to reverse an active ledger movement; a manager decides.</summary>
[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Write)]
public sealed record RequestMovementReversalCommand(int MovementId, string Reason) : IRequest<Result>;
