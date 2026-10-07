using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.ChangePaymentMethod;

[RequirePermission(PermissionFeature.Orders, PermissionLevel.Write)]
public record ChangePaymentMethodCommand(int OrderId, int PaymentMethodId, int ExpectedVersion) : IChangeOrderPaymentMethodCommand;

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

        var valid = order.CheckVersion(request.ExpectedVersion);
        if (!valid.IsSuccess) return valid;
        valid = order.ChangePaymentMethod(request.PaymentMethodId);
        if (!valid.IsSuccess) return valid;
        order.TouchForEdit();
        try { await _unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (PersistenceConflictException exception) { return Result.Failure(exception.Message); }
        catch (PersistenceException) { return Result.Failure("تعذر حفظ طريقة الدفع."); }
        return Result.Success();
    }
}
