using MediatR;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Stores;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.ChangePaymentMethod;

public record ChangePaymentMethodCommand(int OrderId, int PaymentMethodId) : IChangeOrderPaymentMethodCommand;

internal class ChangePaymentMethodCommandHandler : IRequestHandler<ChangePaymentMethodCommand, Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangePaymentMethodCommandHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ChangePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        Order order = await _orderRepository.GetById(request.OrderId);
        if (order == null)
            return Result.Failure("Order not exists.");

        order.ChangePaymentMethod(request.PaymentMethodId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
