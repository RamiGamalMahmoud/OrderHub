using MediatR;
using Microsoft.EntityFrameworkCore;
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
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderItems)
                .ToListAsync(cancellationToken: cancellationToken);

            return orders.Select(o => new OrderListDto(
                    o.Id,
                    o.OrderNumber,
                    new Application.DTOs.CommonDtos.OrderStatusInfoDto( o.OrderStatus.Id, o.OrderStatus.DisplayName, o.OrderStatus.DisplayName),
                    o.Client.Name.Value,
                    o.Client.Phone.Number.FullNumber,
                    o.OrderItems.Count,
                    o.Total.Value,
                    o.CreatedAt
                    ));
        }
        catch (System.Exception)
        {
            return Enumerable.Empty<OrderListDto>();
        }
    }
}
