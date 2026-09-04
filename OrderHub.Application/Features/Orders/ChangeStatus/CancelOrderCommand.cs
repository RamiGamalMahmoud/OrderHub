using MediatR;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Stores;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.ChangeStatus;

public record CancelOrderCommand(int OrderId) : IChangeOrderStatusCommand;

internal class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result<OrderStatus>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelOrderCommandHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<OrderStatus>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        Order order = await _orderRepository.GetById(request.OrderId);

        if (order is null)
            return Result<OrderStatus>.Failure("الطلب غير موجود.");

        order.Cancel();

        await _unitOfWork.SaveChangesAsync();

        return Result<OrderStatus>.Success(order.OrderStatus);
    }
}
