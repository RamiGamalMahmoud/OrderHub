using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Returns.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Inventory.Returns.RecordReturn;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Write)]
public sealed record RecordReturnCommand(
    int OrderId,
    string Note,
    IReadOnlyList<ReturnLineInput> Lines,
    DateTime? EffectiveAtUtc = null) : IRequest<Result<ReturnResult>>;
