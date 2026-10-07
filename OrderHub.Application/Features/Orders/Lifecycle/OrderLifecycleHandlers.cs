using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using OrderHub.Application.Features.Orders.Archive;

namespace OrderHub.Application.Features.Orders.Lifecycle;

internal sealed class OrderLifecycleHandlers(IOrderRepository repository, IUnitOfWork unitOfWork,
    ICurrentUser user, TimeProvider clock) :
    IRequestHandler<ChangeOrderStatusCommand, Result<OrderOperationReceipt>>,
    IRequestHandler<ChangeDeliveryStepStatusCommand, Result<OrderOperationReceipt>>,
    IRequestHandler<DeliverOrderDirectlyCommand, Result<OrderOperationReceipt>>,
    IRequestHandler<ArchiveOrderCommand, Result<OrderOperationReceipt>>
{
    public Task<Result<OrderOperationReceipt>> Handle(ArchiveOrderCommand request, CancellationToken cancellationToken) =>
        Execute(request.OrderId, request.ExpectedVersion,
            (order, operation) => order.Archive(operation, request.Reason), cancellationToken);

    public Task<Result<OrderOperationReceipt>> Handle(ChangeOrderStatusCommand request, CancellationToken cancellationToken) =>
        Execute(request.OrderId, request.ExpectedVersion,
            (order, operation) => order.TransitionTo(request.Target, operation, request.Reason), cancellationToken);

    public Task<Result<OrderOperationReceipt>> Handle(ChangeDeliveryStepStatusCommand request, CancellationToken cancellationToken) =>
        Execute(request.OrderId, request.ExpectedVersion,
            (order, operation) => order.TransitionStep(request.StepId, request.Target, operation, request.Reason), cancellationToken);

    public Task<Result<OrderOperationReceipt>> Handle(DeliverOrderDirectlyCommand request, CancellationToken cancellationToken) =>
        Execute(request.OrderId, request.ExpectedVersion,
            (order, operation) => order.DeliverDirectly(request.Outcomes, operation, request.Reason), cancellationToken);

    private async Task<Result<OrderOperationReceipt>> Execute(int id, int expectedVersion,
        Func<Order, OrderOperation, Result> change, CancellationToken cancellationToken)
    {
        if (!user.IsAuthenticated || user.Id is not > 0)
            return Result<OrderOperationReceipt>.Failure("يجب تسجيل الدخول لتنفيذ العملية.", "Unauthorized");
        var order = await repository.GetForLifecycleAsync(id, cancellationToken);
        if (order is null) return Result<OrderOperationReceipt>.Failure("الطلب غير موجود.", "NotFound");
        var valid = order.CheckVersion(expectedVersion);
        if (!valid.IsSuccess) return Result<OrderOperationReceipt>.Failure(valid.ErrorMessage, "Conflict");
        var operation = new OrderOperation(user.Id.Value, user.DisplayName ?? user.UserName, clock.GetUtcNow().UtcDateTime);
        valid = change(order, operation);
        if (!valid.IsSuccess) return Result<OrderOperationReceipt>.Failure(valid.ErrorMessage, "InvalidOperation");
        try
        {
            if (order.Version != expectedVersion) await unitOfWork.SaveChangesAsync(cancellationToken);
            return new OrderOperationReceipt(order.Id, order.Version, order.OrderStatus);
        }
        catch (PersistenceConflictException exception)
        {
            return Result<OrderOperationReceipt>.Failure(exception.Message, "Conflict");
        }
        catch (PersistenceException)
        {
            return Result<OrderOperationReceipt>.Failure("تعذر حفظ العملية. لم يتم اعتماد أي تغيير.", "Persistence");
        }
    }
}
