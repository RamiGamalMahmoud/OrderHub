using OrderHub.Application.Features.Orders.GetDetails;
using OrderHub.Application.Features.Orders.GetPage;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common.Extensions;
using OrderHub.Application.Common.Lookups;
using OrderHub.Application.Common;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure.Extensions;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace OrderHub.Infrastructure.Features.Orders;

internal sealed partial class OrderReader(AppDbContextFactory contextFactory) : IOrderReader
{
    public async Task<OrderDetails> GetDetailsAsync(int id, CancellationToken cancellationToken)
    {
        using AppDbContext dbContext = contextFactory.CreateDbContext();
        return await dbContext.Orders
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(order => new OrderDetails(
                new OrderDetailsHeader(
                    order.Id,
                    order.OrderNumber,
                    order.CreatedAt,
                    order.OrderStatus.GetDescription()),

                new OrderDetailsClient(
                    order.Client.Name.Value,
                    order.Client.Phone.Number.FullNumber,
                    order.Client.Address.FullAddress),

                new OrderDetailsDelivery(
                    order.DeliveryMethod.HasValue ? order.DeliveryMethod.Value.GetDescription() : "غير محددة",
                    order.Deliveryman == null ? null : order.Deliveryman.Name.Value,
                    order.ShippingCarrier == null ? null : order.ShippingCarrier.Name.Value),

                    order.OrderItems.Select
                    (oi => new OrderDetailsLine(
                        oi.ProductId,
                        oi.ProductName,
                        oi.UnitPrice.Value,
                        oi.Quantity,
                        oi.UnitPrice.Value * oi.Quantity))
                    .ToList(),

                new OrderDetailsPayment(
                    order.PaymentMethod == null ? null : order.PaymentMethod.Description,
                    order.OrderItems.Sum(item => item.UnitPrice.Value * item.Quantity),
                    0,
                    0,
                    order.OrderItems.Sum(item => item.UnitPrice.Value * item.Quantity))

                ))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<OrderListItem>> GetPageAsync(int pageNumber, int pageSize, string searchTerm, DateTime? fromDate, DateTime? toDate, int? paymentMethodId, OrderStatus? orderStatus, CancellationToken cancellationToken, bool archivedOnly = false)
    {
        using AppDbContext dbContext = contextFactory.CreateDbContext();

        var query = dbContext.Orders
            .AsNoTracking()
            .Where(order => order.IsArchived == archivedOnly)
            .ApplyFilter(searchTerm, fromDate, toDate, paymentMethodId, orderStatus)
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id)
            .Select(order => new OrderListItem(
                order.Id,
                order.OrderNumber,
                "",
                order.Client.Name.Value,
                order.Client.Phone.Number.FullNumber,
                order.OrderItems.Count,
                order.OrderItems.Sum(oi => oi.UnitPrice.Value * oi.Quantity),
                order.OrderStatus,

                new EnumItem<OrderStatus>(
                    order.OrderStatus,
                    order.OrderStatus.GetDescription()),

                order.PaymentMethod == null ? null : new OrderPaymentMethodItem(
                    order.PaymentMethod.Id,
                    order.PaymentMethod.DisplayName,
                    order.PaymentMethod.Description,
                    order.PaymentMethod.IsActive),

                order.CreatedAt,
                order.DeliveryMethod,
                order.IsArchived
            ));

        return await query.ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
    }
}
