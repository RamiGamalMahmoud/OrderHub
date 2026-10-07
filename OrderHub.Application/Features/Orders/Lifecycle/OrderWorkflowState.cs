using System;
using System.Collections.Generic;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Orders.Lifecycle;

public sealed record OrderWorkflowState(int Id, string OrderNumber, DateTime CreatedAt, OrderStatus Status,
    DeliveryMethod? Method, string HandlerName, int Version, bool CanEdit, bool CanArchive, bool IsArchived,
    string ReadinessError, IReadOnlyList<OrderStatus> AllowedStatuses, IReadOnlyList<DeliveryStepWorkflowState> Steps,
    DateTime LastChangedAtUtc, string LatestReason, bool IsLegacy, DateTime? ArchivedAtUtc,
    string ArchivedByUserName, string ArchiveReason, bool CanDeliverDirectly = false);

public sealed record DeliveryStepWorkflowState(int Id, int StepOrder, DeliveryMethod Method, string HandlerName,
    DeliveryStatus Status, IReadOnlyList<DeliveryStatus> AllowedStatuses, string BlockingReason,
    DateTime? LastChangedAtUtc, string LastChangedBy, string LatestReason, bool IsLegacy);

public sealed record OrderHistoryEntry(int Id, string PreviousStatus, string NewStatus, DateTime ChangedAtUtc,
    string UserName, string Reason, OrderHistorySource Source, int OrderVersion, string OriginalStatus = null);
