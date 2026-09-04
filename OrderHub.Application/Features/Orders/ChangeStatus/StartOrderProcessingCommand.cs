using MediatR;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Stores;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.ChangeStatus;

public record StartOrderProcessingCommand(int OrderId) : IChangeOrderStatusCommand;

internal class StartOrderProcessingCommandHandler : IRequestHandler<StartOrderProcessingCommand, Result<OrderStatus>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public StartOrderProcessingCommandHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<OrderStatus>> Handle(StartOrderProcessingCommand request, CancellationToken cancellationToken)
    {
        Order order = await _orderRepository.GetById(request.OrderId);

        if (order is null)
            return Result<OrderStatus>.Failure("الطلب غير موجود.");

        order.StartProcessing();

        await _unitOfWork.SaveChangesAsync();

        return Result<OrderStatus>.Success(order.OrderStatus);
    }
}
