using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Orders.GetSummaries;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Features.Orders;

internal sealed partial class OrderReader
{
    public async Task<OrderSummaryData> GetSummariesAsync(int orderId, CancellationToken cancellationToken)
    {
        using var db = contextFactory.CreateDbContext();
        var order = await db.Orders.AsNoTracking()
            .Include(x => x.Client).ThenInclude(x => x.Phone)
            .Include(x => x.Client).ThenInclude(x => x.Address).ThenInclude(x => x.City)
            .Include(x => x.OrderItems)
            .Include(x => x.PaymentMethod)
            .Include(x => x.Deliveryman)
            .Include(x => x.ShippingCarrier).ThenInclude(carrier => carrier.Phone)
            .Include(x => x.DeliveryTrips)
            .Include(x => x.Shipments)
            .Include(x => x.DeliverySteps).ThenInclude(x => x.Deliveryman)
            .Include(x => x.DeliverySteps).ThenInclude(x => x.ShippingCarrier).ThenInclude(carrier => carrier.Phone)
            .Include(x => x.DeliverySteps).ThenInclude(x => x.DeliveryTrips)
            .Include(x => x.DeliverySteps).ThenInclude(x => x.Shipments)
            .SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken);
        if (order is null) return null;

        // Only currently accountable parties are copyable; aborted assignments stay in the audit feed.
        var steps = order.DeliverySteps
            .Where(step => step.StepOrder > 0 && step.Status != DeliveryStatus.Cancelled)
            .OrderBy(step => step.StepOrder).ToList();
        string StepParty(OrderDeliveryStep step) => step.DeliveryMethod == DeliveryMethod.DeliveryMan
            ? step.Deliveryman?.Name.Value : step.ShippingCarrier?.Name.Value;
        var orderPartyId = order.DeliveryMethod switch
        {
            DeliveryMethod.DeliveryMan => order.DeliverymanId,
            DeliveryMethod.ShippingCompany => order.ShippingCarrierId,
            _ => null
        };
        var partyRecords = order.DeliveryTrips
            .Where(record => record.Status != WorkRecordStatus.Cancelled && record.NumberingPartyId == orderPartyId)
            .Select(record => new PartySummaryRecord(
                "مشوار", record.NumberingPartyId, record.WorkRecordNumber, record.SequenceYear, record.SequenceMonth,
                order.Deliveryman?.Name.Value,
                null, null, order.Client.Name.Value,
                order.Deliveryman?.PhoneNumber))
            .Concat(order.Shipments
                .Where(record => record.Status != WorkRecordStatus.Cancelled && record.NumberingPartyId == orderPartyId)
                .Select(record => new PartySummaryRecord(
                    "شحنة", record.NumberingPartyId, record.WorkRecordNumber, record.SequenceYear, record.SequenceMonth,
                    order.ShippingCarrier?.Name.Value,
                    null, null, order.Client.Name.Value,
                    order.ShippingCarrier?.Phone?.Number?.FullNumber))).ToList();

        for (var index = 0; index < steps.Count; index++)
        {
            var step = steps[index];
            var from = index > 0 ? StepParty(steps[index - 1]) : null;
            var to = index + 1 < steps.Count ? StepParty(steps[index + 1]) : order.Client.Name.Value;
            partyRecords.AddRange(step.DeliveryTrips
                .Where(record => record.Status != WorkRecordStatus.Cancelled && record.NumberingPartyId == step.DeliverymanId)
                .Select(record => new PartySummaryRecord(
                    "مشوار", record.NumberingPartyId, record.WorkRecordNumber, record.SequenceYear, record.SequenceMonth,
                    step.Deliveryman?.Name.Value, step.StepOrder, from, to,
                    step.Deliveryman?.PhoneNumber)));
            partyRecords.AddRange(step.Shipments
                .Where(record => record.Status != WorkRecordStatus.Cancelled && record.NumberingPartyId == step.ShippingCarrierId)
                .Select(record => new PartySummaryRecord(
                    "شحنة", record.NumberingPartyId, record.WorkRecordNumber, record.SequenceYear, record.SequenceMonth,
                    step.ShippingCarrier?.Name.Value, step.StepOrder, from, to,
                    step.ShippingCarrier?.Phone?.Number?.FullNumber)));
        }

        var supplies = await db.Supplies.AsNoTracking().Where(x => x.OrderId == orderId)
            .Include(x => x.Supplier).ThenInclude(supplier => supplier.Phone)
            .Include(x => x.Supplier).ThenInclude(supplier => supplier.Address).ThenInclude(address => address.City)
            .ToListAsync(cancellationToken);
        return new OrderSummaryData(order.OrderNumber, order.CreatedAt,
            order.Client.Name.Value, order.Client.Phone?.Number?.FullNumber, order.Client.Address?.FullAddress,
            order.Client.Location, order.Client.Address?.City?.Name.Value,
            order.Total.Value, order.PaymentMethod?.Code, order.PaymentMethod?.DisplayName,
            order.OrderItems.Select(item => new OrderSummaryLine(item.ProductName, item.Quantity, item.UnitPrice.Value, item.VatRate)).ToList(),
            partyRecords.OrderBy(x => x.StepOrder ?? 0).ThenBy(x => x.Number).ToList(),
            supplies.Where(record => order.OrderItems.Any(item => item.SupplierId == record.SupplierId))
                .GroupBy(record => record.SupplierId)
                // Keep one current supplier summary if older data already contains duplicate rows.
                .Select(records => records.OrderByDescending(record => record.CreatedAt).ThenByDescending(record => record.Id).First())
                .Select(record => new SupplierSummaryRecord(record.SupplierId, record.WorkRecordNumber, record.SequenceYear,
                record.SequenceMonth, record.Supplier?.Name.Value,
                order.OrderItems.Where(item => item.SupplierId == record.SupplierId)
                    .Select(item => new OrderSummaryLine(item.ProductName, item.Quantity, item.UnitPrice.Value, item.VatRate)).ToList(),
                record.Supplier?.Phone?.Number?.FullNumber, record.Supplier?.Address?.FullAddress)).ToList());
    }

}
