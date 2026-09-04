using MediatR;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Stores;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.ChangeStatus;

public record DeliverOrderCommand(int OrderId) : IChangeOrderStatusCommand;

internal class DeliverOrderCommandHandler : IRequestHandler<DeliverOrderCommand, Result<OrderStatus>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeliverOrderCommandHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<OrderStatus>> Handle(DeliverOrderCommand request, CancellationToken cancellationToken)
    {
        Order order = await _orderRepository.GetById(request.OrderId);

        if (order is null)
            return Result<OrderStatus>.Failure("الطلب غير موجود.");

        order.Deliver();

        await _unitOfWork.SaveChangesAsync();

        return Result<OrderStatus>.Success(order.OrderStatus);
    }
}
