using MediatR;
using OrderHub.Application.Features.Inventory.Deliveries.Contracts;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.Deliveries.GetOrderDeliveries;

internal sealed class GetOrderDeliveriesQueryHandler(
    IStockDeliveryRepository deliveries,
    IOrderRepository orders) : IRequestHandler<GetOrderDeliveriesQuery, Result<OrderDeliveryOverview>>
{
    public async Task<Result<OrderDeliveryOverview>> Handle(
        GetOrderDeliveriesQuery request,
        CancellationToken cancellationToken)
    {
        Order order = await orders.GetById(request.OrderId);
        if (order is null)
            return Result<OrderDeliveryOverview>.Failure("الطلب غير موجود.", "NotFound");

        OrderDeliveryOverview overview = await deliveries.GetOrderDeliveryOverviewAsync(
            request.OrderId,
            cancellationToken);

        return Result<OrderDeliveryOverview>.Success(overview);
    }
}
