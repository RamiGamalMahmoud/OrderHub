using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Inventory.IssueOrders.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.IssueOrders.CreateStockIssueOrder;

internal sealed class CreateStockIssueOrderCommandHandler(
    IOrderRepository orders,
    IWarehouseRepository warehouses,
    IStockIssueOrderRepository issueOrders,
    IInventoryRepository inventory,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock) : IRequestHandler<CreateStockIssueOrderCommand, Result<int>>
{
    public async Task<Result<int>> Handle(
        CreateStockIssueOrderCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result<int>.Failure("يجب تسجيل الدخول لإنشاء أمر الصرف.", "Unauthorized");

        StockIssueOrderInput input = request.Input;
        if (input is null || input.Lines is null || input.Lines.Count == 0)
            return Result<int>.Failure("أضف بندًا واحدًا على الأقل لأمر الصرف.");
        if (input.Lines.Any(line => line is null || line.AllocationId <= 0 || line.RequestedQty <= 0))
            return Result<int>.Failure("بنود أمر الصرف أو كمياتها غير صالحة.");
        if (input.Lines.Select(line => line.AllocationId).Distinct().Count() != input.Lines.Count)
            return Result<int>.Failure("لا يمكن تكرار التخصيص نفسه في أمر الصرف.");

        Order order = await orders.GetForUpdateAsync(input.OrderId, cancellationToken);
        if (order is null)
            return Result<int>.Failure("الطلب غير موجود.", "NotFound");
        if (order.IsArchived || order.OrderStatus is OrderStatus.Delivered or OrderStatus.Cancelled)
            return Result<int>.Failure("لا يمكن إنشاء أمر صرف لطلب مغلق.", "InvalidOperation");

        Warehouse warehouse = await warehouses.GetByIdAsync(input.WarehouseId, cancellationToken);
        if (warehouse is null)
            return Result<int>.Failure("المخزن غير موجود.", "NotFound");
        if (!warehouse.IsActive)
            return Result<int>.Failure("المخزن غير نشط؛ لا يمكن الصرف منه.");

        int[] allocationIds = input.Lines.Select(line => line.AllocationId).ToArray();
        var allocations = order.Allocations
            .Where(allocation => allocationIds.Contains(allocation.Id))
            .ToDictionary(allocation => allocation.Id);

        if (allocations.Count != allocationIds.Length)
            return Result<int>.Failure("أحد التخصيصات لا يخص هذا الطلب.");

        foreach (StockIssueOrderLineInput line in input.Lines)
        {
            OrderInventoryAllocation allocation = allocations[line.AllocationId];
            if (allocation.Status != OrderAllocationStatus.Active)
                return Result<int>.Failure("لا يمكن الصرف من تخصيص غير نشط.");
            if (allocation.SourceKind != AllocationSourceKind.Warehouse ||
                allocation.WarehouseId != warehouse.Id)
                return Result<int>.Failure("كل بنود أمر الصرف يجب أن تخص المخزن المحدد.");
        }

        DateTime now = clock.GetUtcNow().UtcDateTime;
        var transactionActive = false;
        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);
            transactionActive = true;

            await inventory.LockBalancesAsync(
                allocations.Values
                    .Select(allocation => (allocation.ProductId, allocation.WarehouseId!.Value))
                    .ToList(),
                cancellationToken);

            var activeRequested = await issueOrders.GetActiveRequestedByAllocationAsync(
                allocationIds,
                cancellationToken);
            var cancelRequested = await issueOrders.GetCancelRequestedAllocationIdsAsync(
                allocationIds,
                cancellationToken);

            if (cancelRequested.Count > 0)
            {
                return Result<int>.Failure(
                    "يوجد طلب إلغاء معلق على أحد التخصيصات؛ انتظر قرار المدير قبل إنشاء أمر صرف جديد.",
                    "Conflict");
            }

            foreach (StockIssueOrderLineInput line in input.Lines)
            {
                OrderInventoryAllocation allocation = allocations[line.AllocationId];
                int alreadyRequested = activeRequested.TryGetValue(line.AllocationId, out int requested)
                    ? requested
                    : 0;
                int availableToRequest = allocation.Remaining - alreadyRequested;

                if (line.RequestedQty > availableToRequest)
                {
                    return Result<int>.Failure(
                        $"الكمية المطلوبة تتجاوز المتبقي غير المرتبط بأوامر صرف أخرى في {warehouse.Name}. المتاح: {Math.Max(0, availableToRequest)}.",
                        "Conflict");
                }
            }

            var created = StockIssueOrder.Create(
                order,
                warehouse,
                userId,
                input.Lines
                    .Select(line => new StockIssueLineData(
                        line.AllocationId,
                        allocations[line.AllocationId].ProductId,
                        line.RequestedQty))
                    .ToList(),
                now);

            if (!created.IsSuccess)
                return Result<int>.Failure(created.ErrorMessage);

            issueOrders.Add(created.Value);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            transactionActive = false;
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            return Result<int>.Success(created.Value.Id);
        }
        catch (PersistenceConflictException)
        {
            return Result<int>.Failure(
                "حدث تعارض أثناء حفظ أمر الصرف. يرجى المحاولة مرة أخرى.",
                "Conflict");
        }
        catch (PersistenceException)
        {
            return Result<int>.Failure("تعذر حفظ أمر الصرف. يرجى المحاولة مرة أخرى.", "Persistence");
        }
        finally
        {
            if (transactionActive)
                await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
        }
    }
}
