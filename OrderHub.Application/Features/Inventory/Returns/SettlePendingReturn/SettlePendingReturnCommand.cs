using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Returns.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using System;

namespace OrderHub.Application.Features.Inventory.Returns.SettlePendingReturn;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Write)]
public sealed record SettlePendingReturnCommand(
    int AllocationId,
    int IntactQty,
    int DamagedQty,
    string Note,
    DateTime? EffectiveAtUtc = null) : IRequest<Result<ReturnResult>>;
