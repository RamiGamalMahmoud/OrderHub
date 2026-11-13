using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Inventory.Returns.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.Returns.RecordReturn;

internal sealed class RecordReturnCommandHandler(
    IOrderRepository orders,
    IInventoryReturnRepository returns,
    IInventoryRepository inventory,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock) : IRequestHandler<RecordReturnCommand, Result<ReturnResult>>
{
    public async Task<Result<ReturnResult>> Handle(
        RecordReturnCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result<ReturnResult>.Failure("يجب تسجيل الدخول لتسجيل المرتجع.", "Unauthorized");

        if (request.OrderId <= 0)
            return Result<ReturnResult>.Failure("الطلب غير صالح.");

        if (request.Lines is null || request.Lines.Count == 0)
            return Result<ReturnResult>.Failure("أضف صنفًا واحدًا على الأقل للمرتجع.");

        if (request.Lines.Any(line => line.AllocationId <= 0))
            return Result<ReturnResult>.Failure("أحد التخصيصات المحددة غير صالح.");

        if (request.Lines.Select(line => line.AllocationId).Distinct().Count() != request.Lines.Count)
            return Result<ReturnResult>.Failure("لا يمكن تكرار نفس التخصيص في المرتجع الواحد.");

        if (request.Lines.Any(line => line.IntactQty < 0 || line.DamagedQty < 0))
            return Result<ReturnResult>.Failure("كميات المرتجع لا تكون سالبة.");

        if (request.Lines.All(line => line.IntactQty + line.DamagedQty <= 0))
            return Result<ReturnResult>.Failure("أدخل كمية سليمة أو تالفة أكبر من صفر.");

        Order order = await orders.GetById(request.OrderId);
        if (order is null)
            return Result<ReturnResult>.Failure("الطلب غير موجود.", "NotFound");

        int[] allocationIds = request.Lines.Select(line => line.AllocationId).ToArray();
        IReadOnlyList<OrderInventoryAllocation> allocations = await returns.GetAllocationsForUpdateAsync(
            allocationIds,
            cancellationToken);
        Dictionary<int, OrderInventoryAllocation> allocationsById = allocations.ToDictionary(
            allocation => allocation.Id);

        if (allocationsById.Count != allocationIds.Length)
            return Result<ReturnResult>.Failure("أحد التخصيصات لم يعد موجودًا؛ حدّث الصفحة.", "NotFound");

        if (allocations.Any(allocation => allocation.OrderId != request.OrderId))
            return Result<ReturnResult>.Failure("أحد التخصيصات لا يخص هذا الطلب.", "InvalidOperation");

        if (allocations.Any(allocation => allocation.SourceKind != AllocationSourceKind.Warehouse ||
            allocation.WarehouseId is null))
            return Result<ReturnResult>.Failure("المرتجع متاح لبنود المخزن فقط.", "InvalidOperation");

        int warehouseId = allocations[0].WarehouseId!.Value;
        if (allocations.Any(allocation => allocation.WarehouseId != warehouseId))
            return Result<ReturnResult>.Failure("الأسطر المحددة تخص أكثر من مخزن.");

        Warehouse warehouse = allocations[0].Warehouse;
        if (warehouse is null || !warehouse.IsActive)
            return Result<ReturnResult>.Failure("المخزن غير نشط؛ لا يمكن تسجيل مرتجع عليه.", "Conflict");

        if (warehouse.KeeperUserId != userId)
        {
            return Result<ReturnResult>.Failure(
                "تسجيل المرتجع متاح لأمين المخزن المرتبط بهذا المخزن فقط.",
                "Forbidden");
        }

        DateTime now = clock.GetUtcNow().UtcDateTime;
        DateTime effectiveAtUtc = request.EffectiveAtUtc is DateTime provided && provided != default
            ? DateTime.SpecifyKind(provided, DateTimeKind.Utc)
            : now;

        Result<InventoryReturn> creation = InventoryReturn.Create(
            warehouseId,
            request.OrderId,
            null,
            request.Note,
            userId,
            effectiveAtUtc,
            now);
        if (!creation.IsSuccess)
            return Result<ReturnResult>.Failure(creation.ErrorMessage);

        InventoryReturn record = creation.Value;

        var transactionActive = false;
        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);
            transactionActive = true;

            await inventory.LockBalancesAsync(
                allocations
                    .Select(allocation => (warehouseId, allocation.ProductId))
                    .Distinct()
                    .ToList(),
                cancellationToken);

            int totalIntact = 0;
            int totalDamaged = 0;
            foreach (ReturnLineInput input in request.Lines)
            {
                OrderInventoryAllocation allocation = allocationsById[input.AllocationId];
                int returned = await returns.GetReturnedQuantityAsync(allocation.Id, cancellationToken);
                int remaining = allocation.IssuedQty - returned;
                if (input.IntactQty + input.DamagedQty > remaining)
                {
                    return Result<ReturnResult>.Failure(
                        remaining <= 0
                            ? $"لا توجد كمية متبقية للإرجاع للصنف «{allocation.OrderItem.ProductName}»."
                            : $"كمية المرتجع للصنف «{allocation.OrderItem.ProductName}» تتجاوز المتاح للإرجاع ({remaining.ToString(CultureInfo.InvariantCulture)}).",
                        "Conflict");
                }

                Result<InventoryReturnLine> line = InventoryReturnLine.Create(
                    allocation.Id,
                    allocation.OrderItemId,
                    allocation.ProductId,
                    input.IntactQty,
                    input.DamagedQty);
                if (!line.IsSuccess)
                    return Result<ReturnResult>.Failure(line.ErrorMessage);

                Result attached = record.AddLine(line.Value);
                if (!attached.IsSuccess)
                    return Result<ReturnResult>.Failure(attached.ErrorMessage);

                totalIntact += input.IntactQty;
                totalDamaged += input.DamagedQty;
            }

            returns.Add(record);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            Result applied = await ReturnMovementWriter.ApplyAsync(
                record,
                record.Lines.ToList(),
                inventory,
                userId,
                record.EffectiveAtUtc,
                cancellationToken);
            if (!applied.IsSuccess)
                return Result<ReturnResult>.Failure(applied.ErrorMessage);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            transactionActive = false;
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            return Result<ReturnResult>.Success(
                new ReturnResult(record.Id, totalIntact, totalDamaged, 0));
        }
        catch (PersistenceConflictException)
        {
            return Result<ReturnResult>.Failure(
                "حدث تعارض أثناء تسجيل المرتجع. حدّث الصفحة ثم أعد المحاولة.",
                "Conflict");
        }
        catch (PersistenceException)
        {
            return Result<ReturnResult>.Failure("تعذر تسجيل المرتجع. يرجى المحاولة مرة أخرى.", "Persistence");
        }
        catch (ArgumentException exception)
        {
            return Result<ReturnResult>.Failure(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Result<ReturnResult>.Failure(exception.Message, "Conflict");
        }
        finally
        {
            if (transactionActive)
                await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
        }
    }
}
