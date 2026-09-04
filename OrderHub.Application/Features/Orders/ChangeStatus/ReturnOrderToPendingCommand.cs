using MediatR;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Stores;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.ChangeStatus;

public record ReturnOrderToPendingCommand(int OrderId) : IChangeOrderStatusCommand;

internal class ReturnOrderToPendingCommandHandler : IRequestHandler<ReturnOrderToPendingCommand, Result<OrderStatus>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReturnOrderToPendingCommandHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<OrderStatus>> Handle(ReturnOrderToPendingCommand request, CancellationToken cancellationToken)
    {
        Order order = await _orderRepository.GetById(request.OrderId);

        if (order is null)
            return Result<OrderStatus>.Failure("الطلب غير موجود.");

        order.ReturnToPending();

        await _unitOfWork.SaveChangesAsync();

        return Result<OrderStatus>.Success(order.OrderStatus);
    }
}
