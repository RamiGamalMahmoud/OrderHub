using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Orders.Lifecycle;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Orders.Reassignment;

[RequirePermission(PermissionFeature.Orders, PermissionLevel.Write)]
public sealed record ReassignDeliveryStepCommand(int OrderId, int ExpectedVersion, int StepId, DeliveryMethod Method,
    int? PartyId, string Reason) : IRequest<Result<OrderOperationReceipt>>;

[RequirePermission(PermissionFeature.Orders, PermissionLevel.Write)]
public sealed record CancelDeliveryStepCommand(int OrderId, int ExpectedVersion, int StepId, string Reason)
    : IRequest<Result<OrderOperationReceipt>>;

[RequirePermission(PermissionFeature.Orders, PermissionLevel.Write)]
public sealed record ReassignDirectDeliveryCommand(int OrderId, int ExpectedVersion, DeliveryMethod Method,
    int? PartyId, string Reason) : IRequest<Result<OrderOperationReceipt>>;
