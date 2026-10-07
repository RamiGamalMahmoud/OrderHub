using System;
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
            .SingleOrDefaultAsync(root => root.Id == id, cancellationToken);
        if (order is null) return null;
        var lastOrderEvent = await db.OrderStatusHistories.AsNoTracking().Where(history => history.OrderId == id)
            .OrderByDescending(history => history.OrderVersion).ThenByDescending(history => history.Id).FirstOrDefaultAsync(cancellationToken);
        var lastStepEvents = await db.DeliveryStepStatusHistories.AsNoTracking().Where(history => history.OrderId == id)
            .GroupBy(history => history.DeliveryStepId)
            .Select(group => group.OrderByDescending(history => history.OrderVersion).ThenByDescending(history => history.Id).First())
            .ToListAsync(cancellationToken);
        var steps = order.DeliverySteps.OrderBy(step => step.StepOrder).Select(step =>
        {
            var last = lastStepEvents.SingleOrDefault(history => history.DeliveryStepId == step.Id);
            var allowed = order.AllowedStepStatuses(step);
            var blocker = allowed.Count > 0 || step.Status == DeliveryStatus.Completed ? null
                : order.IsArchived ? "الطلب مؤرشف."
                : order.IsFinal ? "أُغلق تنفيذ الخطوة مع الطلب."
                : order.OrderStatus != OrderStatus.OutForDelivery ? "يجب بدء سلسلة التوصيل أولًا."
                : step.Status == DeliveryStatus.Cancelled ? "الخطوة ملغاة."
                : "بانتظار إكمال الخطوة السابقة أو مراجعة جهة التنفيذ.";
            return new DeliveryStepWorkflowState(step.Id, step.StepOrder, step.DeliveryMethod,
                step.DeliveryMethod == DeliveryMethod.DeliveryMan ? step.Deliveryman?.Name.Value : step.ShippingCarrier?.Name.Value,
                step.Status, allowed, blocker, last?.ChangedAtUtc, last?.ChangedByUserName, last?.Reason,
                last?.Source == OrderHistorySource.LegacyMigration);
        }).ToList();
        var latestStep = lastStepEvents.OrderByDescending(history => history.OrderVersion).ThenByDescending(history => history.ChangedAtUtc).FirstOrDefault();
        var stepIsLatest = latestStep is not null && (lastOrderEvent is null || latestStep.OrderVersion > lastOrderEvent.OrderVersion);
        return new(order.Id, order.OrderNumber, order.CreatedAt, order.OrderStatus, order.DeliveryMethod,
            order.DeliveryMethod == DeliveryMethod.DeliveryMan ? order.Deliveryman?.Name.Value : order.ShippingCarrier?.Name.Value,
            order.Version, order.CanEdit, order.CanArchive, order.IsArchived, order.DeliveryReadinessError(),
            order.AllowedStatuses(), steps, stepIsLatest ? latestStep.ChangedAtUtc : lastOrderEvent?.ChangedAtUtc ?? order.CreatedAt,
            lastOrderEvent?.Reason, (stepIsLatest ? latestStep.Source : lastOrderEvent?.Source) == OrderHistorySource.LegacyMigration,
            order.ArchivedAtUtc, order.ArchivedByUserName, order.ArchiveReason, order.CanDeliverDirectly);
    }

    public async Task<PagedResult<OrderHistoryEntry>> GetHistoryAsync(int orderId, int? stepId,
        int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        using var db = contextFactory.CreateDbContext();
        if (stepId.HasValue)
            return await db.DeliveryStepStatusHistories.AsNoTracking()
                .Where(history => history.OrderId == orderId && history.DeliveryStepId == stepId.Value)
                .OrderByDescending(history => history.OrderVersion).ThenByDescending(history => history.Id)
                .Select(history => new OrderHistoryEntry(history.Id,
                    history.PreviousStatus.HasValue ? history.PreviousStatus.Value.GetDescription() : null,
                    history.NewStatus.GetDescription(), history.ChangedAtUtc, history.ChangedByUserName, history.Reason,
                    history.Source, history.OrderVersion, null)).ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
        return await db.OrderStatusHistories.AsNoTracking().Where(history => history.OrderId == orderId)
            .OrderByDescending(history => history.OrderVersion).ThenByDescending(history => history.Id)
            .Select(history => new OrderHistoryEntry(history.Id,
                history.PreviousStatus.HasValue ? history.PreviousStatus.Value.GetDescription() : null,
                history.NewStatus.GetDescription(), history.ChangedAtUtc, history.ChangedByUserName, history.Reason,
                history.Source, history.OrderVersion, history.OriginalStatus)).ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
    }
}
