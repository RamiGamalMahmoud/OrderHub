using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.ChangeStatus;

[RequirePermission(PermissionFeature.Orders, PermissionLevel.Write)]
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

        return Result<OrderStatus>.Failure("استخدم عمليات دورة الطلب الجديدة؛ تغيير الحالة القديم غير متاح.");
    }
}
