using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Counts.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Inventory.Counts.StartCount;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Write)]
public sealed record StartCountCommand(
    InventoryCountScope Scope,
    IReadOnlyList<int> ProductIds = null) : IRequest<Result<CountStartResult>>;
