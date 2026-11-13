using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using System;

namespace OrderHub.Application.Features.Inventory.Deliveries.RecordStockDelivery;

[RequirePermission(PermissionFeature.Orders, PermissionLevel.Write)]
public sealed record RecordStockDeliveryCommand(
    int OrderId,
    int AllocationId,
    int Quantity,
    int? TripId,
    int? ShipmentId,
    DateTime? EffectiveAtUtc = null,
    string? Note = null) : IRequest<Result>;
