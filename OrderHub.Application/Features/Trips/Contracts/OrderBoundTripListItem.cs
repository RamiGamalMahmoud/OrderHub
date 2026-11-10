#nullable enable

using System;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Trips.Contracts;

public record OrderBoundTripListItem(
    int DeliveryTripId,
    long WorkRecordNumber,
    int SequenceYear,
    int SequenceMonth,
    int? OrderId,
    string? OrderNumber,
    OrderStatus? OrderStatus,
    WorkRecordStatus Status,
    DateTime CreatedAt);
