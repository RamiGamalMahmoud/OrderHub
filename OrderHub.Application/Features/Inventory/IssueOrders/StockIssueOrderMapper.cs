using OrderHub.Application.Features.Inventory.IssueOrders.Contracts;
using OrderHub.Domain.Models;
using System.Linq;

namespace OrderHub.Application.Features.Inventory.IssueOrders;

internal static class StockIssueOrderMapper
{
    public static StockIssueOrderSummary ToSummary(StockIssueOrder order)
    {
        var shortfall = order.Lines
            .SelectMany(line => line.Batches)
            .Where(batch => !string.IsNullOrWhiteSpace(batch.ShortfallReason))
            .OrderByDescending(batch => batch.CreatedAt)
            .ThenByDescending(batch => batch.Id)
            .Select(batch => batch.ShortfallReason)
            .FirstOrDefault();

        return new StockIssueOrderSummary(
            order.Id,
            order.WorkRecordNumber,
            order.SequenceYear,
            order.SequenceMonth,
            order.Warehouse?.Name?.Value ?? "مخزن غير متاح",
            order.Order?.OrderNumber ?? "—",
            order.Status,
            order.CreatedAt,
            DisplayName(order.RequestedBy),
            DisplayName(order.AssignedTo),
            order.Lines.Count,
            order.RequestedTotal,
            order.IssuedTotal,
            shortfall,
            order.CancelReason);
    }

    public static StockIssueOrderDetails ToDetails(StockIssueOrder order) =>
        new(
            order.Id,
            order.WorkRecordNumber,
            order.SequenceYear,
            order.SequenceMonth,
            order.Status,
            order.OrderId,
            order.Order?.OrderNumber ?? "—",
            order.WarehouseId,
            order.Warehouse?.Name?.Value ?? "مخزن غير متاح",
            DisplayName(order.RequestedBy),
            DisplayName(order.AssignedTo),
            order.CreatedAt,
            order.CancelReason,
            order.CancelRequestedBy is null ? null : DisplayName(order.CancelRequestedBy),
            order.CancelRequestedAtUtc,
            false,
            false,
            order.Lines
                .OrderBy(line => line.Id)
                .Select(line => new StockIssueOrderLineDetails(
                    line.Id,
                    line.ProductId,
                    line.Product?.Name?.Value ?? "منتج غير متاح",
                    line.RequestedQty,
                    line.IssuedQty,
                    line.Batches
                        .OrderBy(batch => batch.Id)
                        .Select(batch => new StockIssueBatchDetails(
                            batch.Id,
                            batch.Quantity,
                            DisplayName(batch.RecordedBy),
                            batch.EffectiveAtUtc,
                            batch.ShortfallReason))
                        .ToList()))
                .ToList());

    private static string DisplayName(User user) =>
        user is null ? "—" : user.DisplayName ?? user.UserName ?? "—";
}
