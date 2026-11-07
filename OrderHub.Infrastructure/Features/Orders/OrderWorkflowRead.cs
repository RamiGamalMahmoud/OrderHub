using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Extensions;
using OrderHub.Application.Features.Orders.Lifecycle;
using OrderHub.Domain.Enums;
using OrderHub.Infrastructure.Extensions;

namespace OrderHub.Infrastructure.Features.Orders;

internal sealed partial class OrderReader
{
    public async Task<OrderWorkflowState> GetWorkflowAsync(int id, CancellationToken cancellationToken)
    {
        using var db = contextFactory.CreateDbContext();
        var order = await db.Orders.AsNoTracking()
            .Include(root => root.Deliveryman).Include(root => root.ShippingCarrier)
            .Include(root => root.DeliverySteps).ThenInclude(step => step.Deliveryman)
            .Include(root => root.DeliverySteps).ThenInclude(step => step.ShippingCarrier)
            .Include(root => root.DeliveryMovements)
            .SingleOrDefaultAsync(root => root.Id == id, cancellationToken);
        if (order is null) return null;
        var lastOrderEvent = await db.OrderStatusHistories.AsNoTracking().Where(history => history.OrderId == id)
            .OrderByDescending(history => history.OrderVersion).ThenByDescending(history => history.Id).FirstOrDefaultAsync(cancellationToken);
        var lastStepEvents = await db.DeliveryStepStatusHistories.AsNoTracking().Where(history => history.OrderId == id)
            .GroupBy(history => history.DeliveryStepId)
            .Select(group => group.OrderByDescending(history => history.OrderVersion).ThenByDescending(history => history.Id).First())
            .ToListAsync(cancellationToken);

        var movements = order.DeliveryMovements.OrderBy(movement => movement.OrderVersion).ThenBy(movement => movement.Id).ToList();
        var originalRanks = movements.Where(movement => movement.DeliveryStepId.HasValue && movement.OriginalStepRank.HasValue)
            .GroupBy(movement => movement.DeliveryStepId!.Value)
            .ToDictionary(group => group.Key,
                group => group.OrderByDescending(movement => movement.OrderVersion).ThenByDescending(movement => movement.Id).First().OriginalStepRank!.Value);

        var steps = order.DeliverySteps
            .Select(step => new
            {
                Step = step,
                IsSuperseded = step.StepOrder < 0,
                EffectiveRank = step.StepOrder < 0
                    ? originalRanks.TryGetValue(step.Id, out var originalRank) ? originalRank : Math.Abs(step.StepOrder)
                    : step.StepOrder
            })
            .OrderBy(item => item.EffectiveRank).ThenBy(item => item.Step.Id)
            .Select(item =>
            {
                var step = item.Step;
                var last = lastStepEvents.SingleOrDefault(history => history.DeliveryStepId == step.Id);
                var allowed = order.AllowedStepStatuses(step);
                var blocker = allowed.Count > 0 || step.Status == DeliveryStatus.Completed ? null
                    : item.IsSuperseded || step.Status == DeliveryStatus.Cancelled ? "الخطوة ملغاة أو أُعيد توجيهها."
                    : order.IsArchived ? "الطلب مؤرشف."
                    : order.IsFinal ? "أُغلق تنفيذ الخطوة مع الطلب."
                    : order.OrderStatus != OrderStatus.OutForDelivery ? "يجب بدء سلسلة التوصيل أولًا."
                    : "بانتظار إكمال الخطوة السابقة أو مراجعة جهة التنفيذ.";
                var canReassign = !order.IsArchived && !order.IsFinal &&
                    order.DeliveryMethod == DeliveryMethod.DeliveryChain && !item.IsSuperseded &&
                    step.Status is DeliveryStatus.InProgress or DeliveryStatus.Failed;
                return new DeliveryStepWorkflowState(step.Id, step.StepOrder, step.DeliveryMethod,
                    step.DeliveryMethod == DeliveryMethod.DeliveryMan ? step.Deliveryman?.Name.Value : step.ShippingCarrier?.Name.Value,
                    step.Status, allowed, blocker, last?.ChangedAtUtc, last?.ChangedByUserName, last?.Reason,
                    last?.Source == OrderHistorySource.LegacyMigration, item.IsSuperseded, item.EffectiveRank, canReassign);
            }).ToList();

        var latestStep = lastStepEvents.OrderByDescending(history => history.OrderVersion).ThenByDescending(history => history.ChangedAtUtc).FirstOrDefault();
        var lastMovement = movements.OrderByDescending(movement => movement.OrderVersion)
            .ThenByDescending(movement => movement.ChangedAtUtc).ThenByDescending(movement => movement.Id).FirstOrDefault();
        var lastOrderLike = NewestEvent(lastOrderEvent?.OrderVersion, lastOrderEvent?.ChangedAtUtc,
            lastMovement?.OrderVersion, lastMovement?.ChangedAtUtc);
        var stepIsLatest = latestStep is not null &&
            (lastOrderLike is null || latestStep.OrderVersion > lastOrderLike.Value.Version);
        var newest = stepIsLatest ? latestStep?.Source : lastOrderLike?.IsMovement == true ? lastMovement.Source : lastOrderEvent?.Source;
        var latestReason = lastOrderLike?.IsMovement == true ? lastMovement.Reason : lastOrderEvent?.Reason;

        var activeSteps = order.ActiveSteps;
        return new OrderWorkflowState(order.Id, order.OrderNumber, order.CreatedAt, order.OrderStatus, order.DeliveryMethod,
            HandlerName: order.DeliveryMethod == DeliveryMethod.Pickup ? "استلام من المقر"
                : order.DeliveryMethod == DeliveryMethod.DeliveryMan ? order.Deliveryman?.Name.Value
                : order.ShippingCarrier?.Name.Value,
            order.Version, order.CanEdit, order.CanArchive, order.IsArchived, order.DeliveryReadinessError(),
            order.AllowedStatuses(), steps,
            stepIsLatest ? latestStep!.ChangedAtUtc : lastOrderLike?.ChangedAtUtc ?? order.CreatedAt,
            latestReason, newest == OrderHistorySource.LegacyMigration,
            order.ArchivedAtUtc, order.ArchivedByUserName, order.ArchiveReason,
            activeSteps.Count(step => step.Status != DeliveryStatus.Cancelled),
            activeSteps.Count(step => step.Status == DeliveryStatus.Completed),
            order.CanDeliverDirectly, CanReassignDirectly(order));
    }

    public async Task<PagedResult<OrderHistoryEntry>> GetHistoryAsync(int orderId, int? stepId,
        int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        using var db = contextFactory.CreateDbContext();
        if (stepId.HasValue)
            return await db.DeliveryStepStatusHistories.AsNoTracking()
                .Where(history => history.OrderId == orderId && history.DeliveryStepId == stepId.Value)
                .OrderByDescending(history => history.OrderVersion).ThenByDescending(history => history.Id)
                .Select(history => new OrderHistoryEntry(history.Id, "step:" + history.Id,
                    history.PreviousStatus.HasValue ? history.PreviousStatus.Value.GetDescription() : null,
                    history.NewStatus.GetDescription(), history.ChangedAtUtc, history.ChangedByUserName, history.Reason,
                    history.Source, history.OrderVersion, null, null, null)).ToPagedResultAsync(pageNumber, pageSize, cancellationToken);

        var orderRows = db.OrderStatusHistories.AsNoTracking().Where(history => history.OrderId == orderId)
            .Select(history => new HistoryFeedRow
            {
                Id = history.Id,
                OrderVersion = history.OrderVersion,
                ChangedAtUtc = history.ChangedAtUtc,
                SourceRank = 0,
                PreviousStatus = history.PreviousStatus.HasValue ? history.PreviousStatus.Value.ToString() : null,
                NewStatus = history.NewStatus.ToString(),
                PreviousMethod = null,
                NextMethod = null,
                PreviousPartyName = null,
                NextPartyName = null,
                UserName = history.ChangedByUserName,
                Reason = history.Reason,
                Source = history.Source.ToString(),
                OriginalStatus = history.OriginalStatus
            });
        var movementRows = db.OrderDeliveryMovements.AsNoTracking().Where(movement => movement.OrderId == orderId)
            .Select(movement => new HistoryFeedRow
            {
                Id = movement.Id,
                OrderVersion = movement.OrderVersion,
                ChangedAtUtc = movement.ChangedAtUtc,
                SourceRank = 1,
                PreviousStatus = null,
                NewStatus = null,
                PreviousMethod = movement.PreviousMethod.ToString(),
                NextMethod = movement.NextMethod.ToString(),
                PreviousPartyName = movement.PreviousPartyName,
                NextPartyName = movement.NextPartyName,
                UserName = movement.ChangedByUserName,
                Reason = movement.Reason,
                Source = movement.Source.ToString(),
                OriginalStatus = null
            });
        var feed = orderRows.Concat(movementRows)
            .OrderByDescending(row => row.OrderVersion)
            .ThenByDescending(row => row.ChangedAtUtc)
            .ThenBy(row => row.SourceRank)
            .ThenByDescending(row => row.Id);
        var page = await feed.ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
        return new PagedResult<OrderHistoryEntry>(page.Items.Select(ToHistoryEntry).ToList(), page.TotalCount,
            page.PageNumber, page.PageSize);
    }

    private static OrderHistoryEntry ToHistoryEntry(HistoryFeedRow row)
    {
        var isMovement = row.SourceRank == 1;
        var source = ParseEnum<OrderHistorySource>(row.Source) ?? OrderHistorySource.ManualAction;
        var previousMethod = ParseEnum<DeliveryMethod>(row.PreviousMethod);
        var nextMethod = ParseEnum<DeliveryMethod>(row.NextMethod);
        var displayTag = source switch
        {
            OrderHistorySource.DeliveryRouteReassigned => "إعادة توجيه",
            OrderHistorySource.PickupConfirmed => "تأكيد استلام",
            _ => null
        };
        return new OrderHistoryEntry(row.Id, (isMovement ? "movement:" : "order:") + row.Id,
            isMovement ? RouteLabel(previousMethod, row.PreviousPartyName)
                : ParseEnum<OrderStatus>(row.PreviousStatus)?.GetDescription(),
            isMovement ? RouteLabel(nextMethod, row.NextPartyName) : ParseEnum<OrderStatus>(row.NewStatus)?.GetDescription(),
            row.ChangedAtUtc, row.UserName, row.Reason, source, row.OrderVersion, row.OriginalStatus,
            displayTag, isMovement ? $"{RouteLabel(previousMethod, row.PreviousPartyName)} ← {RouteLabel(nextMethod, row.NextPartyName)}" : null);
    }

    private static T? ParseEnum<T>(string value) where T : struct, Enum =>
        !string.IsNullOrWhiteSpace(value) && Enum.TryParse<T>(value, out var parsed) ? parsed : null;

    private static string RouteLabel(DeliveryMethod? method, string partyName) => method switch
    {
        DeliveryMethod.Pickup => "استلام من المقر",
        null => null,
        _ => string.IsNullOrWhiteSpace(partyName) ? method.Value.GetDescription() : $"{method.Value.GetDescription()} — {partyName}"
    };

    private static bool CanReassignDirectly(OrderHub.Domain.Models.Order order)
    {
        if (order.IsArchived || order.IsFinal) return false;
        var hasParty = order.DeliveryMethod switch
        {
            DeliveryMethod.DeliveryMan => order.DeliverymanId is > 0,
            DeliveryMethod.ShippingCompany => order.ShippingCarrierId is > 0,
            _ => false
        };
        return hasParty && order.OrderStatus is OrderStatus.OutForDelivery or OrderStatus.Shipped or OrderStatus.DeliveryFailed;
    }

    private static (int Version, DateTime ChangedAtUtc, bool IsMovement)? NewestEvent(int? orderVersion, DateTime? orderAtUtc,
        int? movementVersion, DateTime? movementAtUtc)
    {
        if (orderVersion is null) return movementVersion is null ? null : (movementVersion.Value, movementAtUtc!.Value, true);
        if (movementVersion is null) return (orderVersion.Value, orderAtUtc!.Value, false);
        return movementVersion.Value > orderVersion.Value ||
               movementVersion.Value == orderVersion.Value && movementAtUtc!.Value > orderAtUtc!.Value
            ? (movementVersion.Value, movementAtUtc.Value, true)
            : (orderVersion.Value, orderAtUtc.Value, false);
    }

    private sealed class HistoryFeedRow
    {
        public int Id { get; init; }
        public int OrderVersion { get; init; }
        public DateTime ChangedAtUtc { get; init; }
        public int SourceRank { get; init; }
        public string PreviousStatus { get; init; }
        public string NewStatus { get; init; }
        public string PreviousMethod { get; init; }
        public string NextMethod { get; init; }
        public string PreviousPartyName { get; init; }
        public string NextPartyName { get; init; }
        public string UserName { get; init; }
        public string Reason { get; init; }
        public string Source { get; init; }
        public string OriginalStatus { get; init; }
    }
}
