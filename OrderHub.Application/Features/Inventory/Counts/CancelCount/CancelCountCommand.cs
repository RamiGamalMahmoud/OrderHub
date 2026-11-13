using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Counts.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.Counts.CancelCount;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Write)]
public sealed record CancelCountCommand(
    int CountId,
    string Note) : IRequest<Result<CountDecisionResult>>;
