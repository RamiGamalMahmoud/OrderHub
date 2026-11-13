using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Counts.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.Counts.CompleteCount;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Write)]
public sealed record CompleteCountCommand(int CountId) : IRequest<Result<CountDecisionResult>>;
