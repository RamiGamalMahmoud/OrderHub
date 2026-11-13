using MediatR;
using OrderHub.Application.Features.Inventory.Returns.Contracts;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.Returns.GetOrderReturnContext;

internal sealed class GetOrderReturnContextQueryHandler(
    IOrderRepository orders,
    IInventoryReturnRepository returns) : IRequestHandler<GetOrderReturnContextQuery, Result<OrderReturnContext>>
{
    public async Task<Result<OrderReturnContext>> Handle(
        GetOrderReturnContextQuery request,
        CancellationToken cancellationToken)
    {
        if (request.OrderId <= 0)
            return Result<OrderReturnContext>.Failure("الطلب غير صالح.");

        Order order = await orders.GetById(request.OrderId);
        if (order is null)
            return Result<OrderReturnContext>.Failure("الطلب غير موجود.", "NotFound");

        OrderReturnContext context = await returns.GetOrderReturnContextAsync(
            request.OrderId,
            cancellationToken);

        return Result<OrderReturnContext>.Success(context);
    }
}
