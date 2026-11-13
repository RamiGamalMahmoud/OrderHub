using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Inventory.Returns.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.Returns.SettlePendingReturn;

internal sealed class SettlePendingReturnCommandHandler(
    IInventoryReturnRepository returns,
    IInventoryRepository inventory,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock) : IRequestHandler<SettlePendingReturnCommand, Result<ReturnResult>>
{
    public async Task<Result<ReturnResult>> Handle(
        SettlePendingReturnCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result<ReturnResult>.Failure("يجب تسجيل الدخول لتسجيل المرتجع.", "Unauthorized");

        if (request.AllocationId <= 0)
            return Result<ReturnResult>.Failure("التخصيص غير صالح.");

        if (request.IntactQty < 0 || request.DamagedQty < 0)
            return Result<ReturnResult>.Failure("كميات المرتجع لا تكون سالبة.");

        int total = request.IntactQty + request.DamagedQty;
        if (total <= 0)
            return Result<ReturnResult>.Failure("أدخل كمية سليمة أو تالفة أكبر من صفر.");

        OrderInventoryAllocation allocation = (await returns.GetAllocationsForUpdateAsync(
            [request.AllocationId],
            cancellationToken)).FirstOrDefault();
        if (allocation is null)
            return Result<ReturnResult>.Failure("التخصيص لم يعد موجودًا؛ حدّث الصفحة.", "NotFound");

        if (allocation.SourceKind != AllocationSourceKind.Warehouse || allocation.WarehouseId is not int warehouseId)
            return Result<ReturnResult>.Failure("المرتجع متاح لبنود المخزن فقط.", "InvalidOperation");

        Warehouse warehouse = allocation.Warehouse;
        if (warehouse is null || !warehouse.IsActive)
            return Result<ReturnResult>.Failure("المخزن غير نشط؛ لا يمكن تسجيل مرتجع عليه.", "Conflict");

        if (warehouse.KeeperUserId != userId)
        {
            return Result<ReturnResult>.Failure(
                "تسجيل المرتجع متاح لأمين المخزن المرتبط بهذا المخزن فقط.",
                "Forbidden");
        }

        if (total > allocation.PendingReturnQty)
        {
            return Result<ReturnResult>.Failure(
                $"كمية الاستلام تتجاوز المرتجع المستحق على هذا الصنف ({allocation.PendingReturnQty.ToString(CultureInfo.InvariantCulture)}).",
                "Conflict");
        }

        DateTime now = clock.GetUtcNow().UtcDateTime;
        DateTime effectiveAtUtc = request.EffectiveAtUtc is DateTime provided && provided != default
            ? DateTime.SpecifyKind(provided, DateTimeKind.Utc)
            : now;

        Result<InventoryReturn> creation = InventoryReturn.Create(
            warehouseId,
            allocation.OrderId,
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
                [(warehouseId, allocation.ProductId)],
                cancellationToken);

            int returned = await returns.GetReturnedQuantityAsync(allocation.Id, cancellationToken);
            int remaining = allocation.IssuedQty - returned;
            if (total > remaining)
            {
                return Result<ReturnResult>.Failure(
                    remaining <= 0
                        ? "لا توجد كمية متبقية للإرجاع لهذا الصنف."
                        : $"كمية الاستلام تتجاوز المتاح للإرجاع ({remaining.ToString(CultureInfo.InvariantCulture)}).",
                    "Conflict");
            }

            Result settled = allocation.SettlePendingReturn(total);
            if (!settled.IsSuccess)
                return Result<ReturnResult>.Failure(settled.ErrorMessage, "Conflict");

            Result<InventoryReturnLine> line = InventoryReturnLine.Create(
                allocation.Id,
                allocation.OrderItemId,
                allocation.ProductId,
                request.IntactQty,
                request.DamagedQty,
                pendingSettledQty: total);
            if (!line.IsSuccess)
                return Result<ReturnResult>.Failure(line.ErrorMessage);

            Result attached = record.AddLine(line.Value);
            if (!attached.IsSuccess)
                return Result<ReturnResult>.Failure(attached.ErrorMessage);

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
                new ReturnResult(record.Id, request.IntactQty, request.DamagedQty, total));
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
