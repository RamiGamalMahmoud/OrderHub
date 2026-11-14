using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Orders.Allocations;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;

namespace OrderHub.Application.Features.Orders.Lifecycle;

internal sealed class OrderLifecycleHandlers(IOrderRepository repository, IUnitOfWork unitOfWork,
    IInventoryRepository inventory, IStockIssueOrderRepository issueOrders, ICurrentUser user,
    TimeProvider clock) :
    IRequestHandler<ChangeOrderStatusCommand, Result<OrderOperationReceipt>>,
    IRequestHandler<ChangeDeliveryStepStatusCommand, Result<OrderOperationReceipt>>,
    IRequestHandler<DeliverOrderDirectlyCommand, Result<OrderOperationReceipt>>
{
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

        var closes = order.OrderStatus is OrderStatus.Delivered or OrderStatus.Cancelled;
        var releaseable = closes ? order.OpenAllocations() : [];
        var activeIssueOrders = closes
            ? await issueOrders.GetActiveForOrderAsync(order.Id, cancellationToken)
            : [];
        var transactionActive = false;
        try
        {
            if (releaseable.Count > 0 || activeIssueOrders.Count > 0)
            {
                await unitOfWork.BeginTransactionAsync(cancellationToken);
                transactionActive = true;

                var reason = order.OrderStatus == OrderStatus.Cancelled ? "إلغاء الطلب"
                    : "إتمام التسليم";
                var now = clock.GetUtcNow().UtcDateTime;
                foreach (var issueOrder in activeIssueOrders)
                {
                    var cancelled = issueOrder.Cancel($"إلغاء تلقائي: {reason}", now);
                    if (!cancelled.IsSuccess)
                        return Result<OrderOperationReceipt>.Failure(cancelled.ErrorMessage, "Conflict");
                }

                var releases = releaseable
                    .Where(allocation => allocation.SourceKind == AllocationSourceKind.Warehouse &&
                        allocation.WarehouseId.HasValue && allocation.Remaining > 0)
                    .Select(allocation => (ProductId: allocation.ProductId,
                        WarehouseId: allocation.WarehouseId!.Value, Qty: allocation.Remaining))
                    .ToList();
                if (releases.Count > 0)
                {
                    await inventory.LockBalancesAsync(
                        releases.Select(release => (release.WarehouseId, release.ProductId)).Distinct().ToList(),
                        cancellationToken);
                    var released = await OrderAllocationReservation.ReleaseAsync(inventory, releases, cancellationToken);
                    if (!released.IsSuccess)
                        return Result<OrderOperationReceipt>.Failure(released.ErrorMessage, "Conflict");
                }

                if (releaseable.Count > 0)
                {
                    valid = order.ReleaseOpenAllocations(operation, reason);
                    if (!valid.IsSuccess)
                        return Result<OrderOperationReceipt>.Failure(valid.ErrorMessage, "InvalidOperation");
                }
            }

            if (order.Version != expectedVersion || transactionActive)
                await unitOfWork.SaveChangesAsync(cancellationToken);
            if (transactionActive)
            {
                transactionActive = false;
                await unitOfWork.CommitTransactionAsync(cancellationToken);
            }
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
        finally
        {
            if (transactionActive)
                await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
        }
    }
}
