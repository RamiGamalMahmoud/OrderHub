using System.Collections.Generic;
using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;

namespace OrderHub.Application.Features.Orders.Lifecycle;

[RequirePermission(PermissionFeature.Orders, PermissionLevel.Write)]
public sealed record ChangeOrderStatusCommand(int OrderId, int ExpectedVersion, OrderStatus Target, string Reason = null)
    : IRequest<Result<OrderOperationReceipt>>;

[RequirePermission(PermissionFeature.Orders, PermissionLevel.Write)]
public sealed record ChangeDeliveryStepStatusCommand(int OrderId, int ExpectedVersion, int StepId, DeliveryStatus Target, string Reason = null)
    : IRequest<Result<OrderOperationReceipt>>;

[RequirePermission(PermissionFeature.Orders, PermissionLevel.Write)]
public sealed record DeliverOrderDirectlyCommand(int OrderId, int ExpectedVersion, IReadOnlyList<DeliveryStepOutcome> Outcomes, string Reason)
    : IRequest<Result<OrderOperationReceipt>>;

public sealed record OrderOperationReceipt(int OrderId, int Version, OrderStatus Status);
