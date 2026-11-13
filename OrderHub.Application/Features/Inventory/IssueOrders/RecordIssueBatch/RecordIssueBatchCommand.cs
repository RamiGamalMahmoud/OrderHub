using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.IssueOrders.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Inventory.IssueOrders.RecordIssueBatch;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Write)]
public sealed record RecordIssueBatchCommand(
    int IssueOrderId,
    bool Final,
    DateTime EffectiveAtUtc,
    IReadOnlyList<IssueBatchLineInput> Lines) : IRequest<Result>;
