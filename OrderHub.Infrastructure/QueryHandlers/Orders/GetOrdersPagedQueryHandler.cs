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
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderListDto(
                o.Id,
                o.OrderNumber,
                o.Client.Name.Value,
                o.Client.Phone.Number.FullNumber,
                o.OrderItems.Count,
                o.OrderItems.Sum(oi => oi.SubTotal.Value),
                o.OrderStatus,
                new EnumItem<OrderStatus>(o.OrderStatus, o.OrderStatus.GetDescription()),
                o.PaymentMethod == null ? null : new Application.DTOs.PaymentMothodsDtos.PaymentMethodListDto(o.PaymentMethod.Id, o.PaymentMethod.DisplayName, o.PaymentMethod.Description, o.PaymentMethod.IsActive),
                o.CreatedAt
            ));

        return await query.ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);
    }
}
