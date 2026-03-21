using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common;
using OrderHub.Domain.Enums;
using OrderHub.Infrastructure.Extensions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.OrderDtos;
using static OrderHub.Application.Queries.OrderQueries;

namespace OrderHub.Infrastructure.QueryHandlers.Orders;

internal class GetOrdersPagedQueryHandler(AppDbContextFactory appDbContextFactory) 
    : IRequestHandler<GetOrdersPagedQuery, PagedResult<OrderListDto>>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<PagedResult<OrderListDto>> Handle(GetOrdersPagedQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        var query = appDbContext.Orders
            .AsNoTracking()
            .Where(o =>
                (string.IsNullOrWhiteSpace(request.SearchTerm) || o.Client.Name.Value.Contains(request.SearchTerm))
                && (!request.FromDate.HasValue || o.CreatedAt >= request.FromDate.Value.Date)
                && (!request.ToDate.HasValue || o.CreatedAt < request.ToDate.Value.Date.AddDays(1)))
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderListDto(
                o.Id,
                o.OrderNumber,
                o.Client.Name.Value,
                o.Client.Phone.Number.FullNumber,
                o.OrderItems.Count,
                o.OrderItems.Sum(oi => oi.UnitPrice.Value * oi.Quantity),
                o.OrderStatus,
                new EnumItem<OrderStatus>(o.OrderStatus, o.OrderStatus.GetDescription()),
                o.PaymentMethod == null ? null : new Application.DTOs.PaymentMothodsDtos.PaymentMethodListDto(o.PaymentMethod.Id, o.PaymentMethod.DisplayName, o.PaymentMethod.Description, o.PaymentMethod.IsActive),
                o.CreatedAt,
                true,
                o.OrderItems.Any(item => item.SupplierId != null),
                o.ShippingCarrierId != null || o.DeliverySteps.Any(step => step.ShippingCarrierId != null),
                o.DeliverymanId != null || o.DeliverySteps.Any(step => step.DeliverymanId != null),
                o.OutboxMessages.Any(message => message.RecipientType == RecipientType.Client && message.Status == OutboxMessageStatus.Sent),
                o.OutboxMessages.Any(message => message.RecipientType == RecipientType.Supplier && message.Status == OutboxMessageStatus.Sent),
                o.OutboxMessages.Any(message => message.RecipientType == RecipientType.ShippingCarrier && message.Status == OutboxMessageStatus.Sent),
                o.OutboxMessages.Any(message => message.RecipientType == RecipientType.Deliveryman && message.Status == OutboxMessageStatus.Sent)
            ));

        return await query.ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);
    }
}
