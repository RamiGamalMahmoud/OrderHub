using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.OrderDtos;
using static OrderHub.Application.Queries.OrderQueries;

namespace OrderHub.Infrastructure.QueryHandlers.Orders;

internal class GetOrdersQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetOrdersQuery, IEnumerable<OrderListDto>>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<IEnumerable<OrderListDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        try
        {
            IEnumerable<Order> orders = await appDbContext.Orders
                .Include(o => o.Client)
                    .ThenInclude(c => c.Phone)
                .Include(o => o.PaymentMethod)
                .Include(o => o.OrderItems)
                .Include(o => o.DeliverySteps)
                .Include(o => o.EntitySequences)
                .Include(o => o.OutboxMessages)
                .ToListAsync(cancellationToken: cancellationToken);

            return orders.Select(o => new OrderListDto(
                    o.Id,
                    o.OrderNumber,
                    o.GetDisplayTitle(RecipientType.Client, o.ClientId),
                    o.Client.Name.Value,
                    o.Client.Phone.Number.FullNumber,
                    o.OrderItems.Count,
                    o.Total.Value,
                    o.OrderStatus,
                    new EnumItem<OrderStatus>(o.OrderStatus, o.OrderStatus.GetDescription()),
                    o.PaymentMethod == null ? null : new Application.DTOs.PaymentMothodsDtos.PaymentMethodListDto(o.PaymentMethod.Id, o.PaymentMethod.DisplayName, o.PaymentMethod.Description, o.PaymentMethod.IsActive),
                    o.CreatedAt,
                    true,
                    o.OrderItems.Any(item => item.SupplierId is not null),
                    o.ShippingCarrierId is not null || o.DeliverySteps.Any(step => step.ShippingCarrierId is not null),
                    o.DeliverymanId is not null || o.DeliverySteps.Any(step => step.DeliverymanId is not null),
                    o.OutboxMessages.Any(message => message.RecipientType == RecipientType.Client && message.Status == Domain.Enums.OutboxMessageStatus.Sent),
                    o.OutboxMessages.Any(message => message.RecipientType == RecipientType.Supplier && message.Status == Domain.Enums.OutboxMessageStatus.Sent),
                    o.OutboxMessages.Any(message => message.RecipientType == RecipientType.ShippingCarrier && message.Status == Domain.Enums.OutboxMessageStatus.Sent),
                    o.OutboxMessages.Any(message => message.RecipientType == RecipientType.Deliveryman && message.Status == Domain.Enums.OutboxMessageStatus.Sent)
                    ));
        }
        catch (System.Exception)
        {
            return Enumerable.Empty<OrderListDto>();
        }
    }
}
