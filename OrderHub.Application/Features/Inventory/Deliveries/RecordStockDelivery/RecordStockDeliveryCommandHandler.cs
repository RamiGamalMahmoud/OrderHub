using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;

namespace OrderHub.Application.Features.Inventory.Deliveries.RecordStockDelivery;

internal sealed class RecordStockDeliveryCommandHandler(
    IOrderRepository orders,
    IStockDeliveryRepository deliveries,
    IInventoryRepository inventory,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock) : IRequestHandler<RecordStockDeliveryCommand, Result>
{
    public async Task<Result> Handle(
        RecordStockDeliveryCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result.Failure("يجب تسجيل الدخول لتسجيل تسليم المخزون.", "Unauthorized");

        if (request.Quantity <= 0)
            return Result.Failure("كمية التسليم يجب أن تكون أكبر من صفر.");

        Order order = await orders.GetForUpdateAsync(request.OrderId, cancellationToken);
        if (order is null)
            return Result.Failure("الطلب غير موجود.", "NotFound");

        if (order.OrderStatus == OrderStatus.Cancelled)
            return Result.Failure("الطلب ملغى ولا يقبل تسليمات جديدة.", "Conflict");

        OrderInventoryAllocation allocation = order.Allocations
            .FirstOrDefault(candidate => candidate.Id == request.AllocationId);
        if (allocation is null)
            return Result.Failure("بند التخصيص غير موجود في هذا الطلب.", "NotFound");

        if (allocation.Status != OrderAllocationStatus.Active)
            return Result.Failure("التخصيص لم يعد نشطًا ولا يقبل تسليمات جديدة.", "Conflict");

        if (allocation.SourceKind != AllocationSourceKind.Warehouse || allocation.WarehouseId is not int warehouseId)
            return Result.Failure("تسليم المخزون متاح لبنود المخزن فقط.", "InvalidOperation");

        if (allocation.IssuedQty <= 0)
            return Result.Failure("لم تُصرف كمية لهذا البند بعد، فلا يوجد ما يُسلَّم.", "Conflict");

        if (!await deliveries.IsLinkValidAsync(request.OrderId, request.TripId, request.ShipmentId, cancellationToken))
            return Result.Failure("المشوار أو الشحنة المحددة لا تخص هذا الطلب.", "InvalidOperation");

        DateTime now = clock.GetUtcNow().UtcDateTime;
        DateTime effectiveAtUtc = request.EffectiveAtUtc is DateTime provided && provided != default
            ? DateTime.SpecifyKind(provided, DateTimeKind.Utc)
            : now;

        var transactionActive = false;
        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);
            transactionActive = true;

            await inventory.LockBalancesAsync(
                [(warehouseId, allocation.ProductId)],
                cancellationToken);

            int delivered = await deliveries.GetDeliveredQuantityAsync(allocation.Id, cancellationToken);
            int remaining = allocation.IssuedQty - delivered;
            if (request.Quantity > remaining)
            {
                return Result.Failure(
                    remaining <= 0
                        ? "سُلمت كل الكمية المصروفة لهذا البند بالفعل."
                        : $"كمية التسليم تتجاوز المتبقي ({remaining.ToString(CultureInfo.InvariantCulture)}).",
                    "Conflict");
            }

            Result<StockDeliveryRecord> created = StockDeliveryRecord.Create(
                order.Id,
                allocation.OrderItemId,
                allocation.Id,
                allocation.ProductId,
                request.Quantity,
                request.TripId,
                request.ShipmentId,
                request.Note,
                userId,
                effectiveAtUtc,
                now);
            if (!created.IsSuccess)
                return Result.Failure(created.ErrorMessage);

            deliveries.Add(created.Value);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            transactionActive = false;
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            return Result.Success();
        }
        catch (ArgumentException exception)
        {
            return Result.Failure(exception.Message);
        }
        catch (PersistenceConflictException)
        {
            return Result.Failure("حدث تعارض أثناء تسجيل التسليم. حدّث الطلب ثم أعد المحاولة.", "Conflict");
        }
        catch (PersistenceException)
        {
            return Result.Failure("تعذر تسجيل التسليم. يرجى المحاولة مرة أخرى.", "Persistence");
        }
        finally
        {
            if (transactionActive)
                await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
        }
    }
}
