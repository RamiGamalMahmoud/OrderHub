using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Enums;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.OrderDtos;
using static OrderHub.Application.Queries.OrderQueries;

namespace OrderHub.Infrastructure.QueryHandlers.Orders;

internal class GetOrderDetailsQueryHandler(AppDbContextFactory dbContextFactory) : IRequestHandler<GetOrderDetailsQuery, OrderDetailsDto>
{
    public async Task<OrderDetailsDto> Handle(GetOrderDetailsQuery request, CancellationToken cancellationToken)
    {
        using(AppDbContext dbContext = dbContextFactory.CreateDbContext())
        {
            var order = await dbContext.Orders
                .AsNoTracking()
                .Where(o => o.Id == request.OrderId)
                .Select(o => new OrderDetailsDto(
                    new OrderHeaderDto(
                        o.Id, 
                        o.OrderNumber, 
                        o.CreatedAt, 
                        o.OrderStatus.GetDescription()),

                    new CustomerInfoDto(
                        o.Client.Name.Value, 
                        o.Client.Phone.Number.FullNumber, 
                        o.Client.Address.FullAddress),

                    new DeliveryInfoDto(
                        o.DeliveryMethod.GetDescription(), 
                        o.Deliveryman.Name.Value, 
                        o.ShippingCarrier.Name.Value),

                    new OrderItemsInfoDto(
                        o.OrderItems.Select
                        (oi => new OrderItemInfoDto(
                            oi.Id, 
                            oi.ProductName, 
                            oi.UnitPrice.Value, 
                            oi.Quantity, 
                            oi.SubTotal.Value))
                        .ToList()),

                    new OrderPaymentInfoDto(
                        o.PaymentMethod.Description, 
                        o.Total.Value, 
                        0, 
                        0, 
                        o.Total.Value)

                    )).FirstOrDefaultAsync(cancellationToken);
                
            return order;
        }
    }
}
