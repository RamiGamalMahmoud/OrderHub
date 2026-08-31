using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common.Extensions;
using OrderHub.Application.Features.Orders.Queries;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Features.Orders.Queries;

internal class GetOrderDetialsQeuryHandler(AppDbContextFactory dbContextFactory) : IRequestHandler<GetOrderDetials.Query, GetOrderDetials.Order>
{
    public async Task<GetOrderDetials.Order> Handle(GetOrderDetials.Query request, CancellationToken cancellationToken)
    {
        using AppDbContext dbContext = dbContextFactory.CreateDbContext();
        return await dbContext.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .Where(o => o.Id == request.Id)
            .Select(order => new GetOrderDetials.Order(
                new GetOrderDetials.Header(
                    order.Id,
                    order.OrderNumber,
                    order.CreatedAt,
                    order.OrderStatus.GetDescription()),

                new GetOrderDetials.Client(
                    order.Client.Name.Value,
                    order.Client.Phone.Number.FullNumber,
                    order.Client.Address.FullAddress),

                new GetOrderDetials.Delivery(
                    order.DeliveryMethod.GetDescription(),
                    order.Deliveryman.Name.Value,
                    order.ShippingCarrier.Name.Value),

                    order.OrderItems.Select
                    (oi => new GetOrderDetials.Item(
                        oi.ProductId,
                        oi.ProductName,
                        oi.UnitPrice.Value,
                        oi.Quantity,
                        oi.SubTotal.Value))
                    .ToList(),

                new GetOrderDetials.Payment(
                    order.PaymentMethod.Description,
                    order.Total.Value,
                    0,
                    0,
                    order.Total.Value)

                ))
            .SingleAsync(cancellationToken);
    }
}
