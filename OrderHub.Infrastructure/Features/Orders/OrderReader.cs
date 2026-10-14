using OrderHub.Application.Features.Orders.GetDetails;
using OrderHub.Application.Features.Orders.GetPage;
using OrderHub.Application.Features.Orders.GetSummaries;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common.Extensions;
using OrderHub.Application.Common.Lookups;
using OrderHub.Application.Common;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.Services.Pricing;
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
        var order = await dbContext.Orders.AsNoTracking()
            .Include(item => item.Client).ThenInclude(client => client.Phone)
            .Include(item => item.Client).ThenInclude(client => client.Address).ThenInclude(address => address.City)
            .Include(item => item.Deliveryman)
            .Include(item => item.ShippingCarrier)
            .Include(item => item.PaymentMethod)
            .Include(item => item.OrderItems)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (order is null) return null;

        var pricing = order.Pricing;
        var lines = pricing.Items.Select(item => new OrderDetailsLine(
            item.ProductId, item.ProductName, item.UnitPrice, (int)item.Quantity, item.VatRate,
            item.Subtotal, item.VatAmount, item.Total)).ToList();
        return new OrderDetails(
            new OrderDetailsHeader(order.Id, order.OrderNumber, order.CreatedAt, order.OrderStatus.GetDescription()),
            new OrderDetailsClient(order.Client.Name.Value, order.Client.Phone.Number.FullNumber, order.Client.Address.FullAddress),
            new OrderDetailsDelivery(order.DeliveryMethod.HasValue ? order.DeliveryMethod.Value.GetDescription() : "غير محددة",
                order.Deliveryman?.Name.Value, order.ShippingCarrier?.Name.Value),
            lines,
            new OrderDetailsPayment(order.PaymentMethod?.Description, pricing.Subtotal, pricing.TotalVat, 0, 0, pricing.TotalAmount));
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
                order.OrderItems.Sum(oi => oi.UnitPrice.Value * oi.Quantity + oi.UnitPrice.Value * oi.Quantity * oi.VatRate / 100m),
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
