using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Context;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.Approvals.RequestMovementReversal;

internal sealed class RequestMovementReversalCommandHandler(
    IInventoryRepository inventory,
    IWarehouseRepository warehouses,
    IAuthorizationService authorization,
    IInventoryApprovalRepository approvals,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock) : IRequestHandler<RequestMovementReversalCommand, Result>
{
    public async Task<Result> Handle(
        RequestMovementReversalCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result.Failure("يجب تسجيل الدخول لطلب عكس الحركة.", "Unauthorized");

        InventoryMovement movement = await inventory.GetMovementForUpdateAsync(
            request.MovementId,
            cancellationToken);
        if (movement is null)
            return Result.Failure("الحركة غير موجودة.", "NotFound");

        if (movement.Status != InventoryMovementStatus.Active)
            return Result.Failure("لا يمكن طلب عكس حركة غير نشطة.", "Conflict");

        if (movement.MovementType == InventoryMovementType.Reversal)
            return Result.Failure("لا يمكن طلب عكس حركة عكسية.", "Conflict");

        bool isKeeper = await warehouses.GetByKeeperAsync(userId, cancellationToken) is { Id: var warehouseId } &&
            warehouseId == movement.WarehouseId;
        if (!isKeeper && !await authorization.IsAllowedAsync(
                PermissionFeature.Inventory,
                PermissionLevel.Approve,
                cancellationToken))
        {
            return Result.Failure(
                "طلب عكس الحركة متاح لأمين المخزن المرتبط بهذا المخزن أو المدير المختص.",
                "Forbidden");
        }

        if (await approvals.HasPendingReversalAsync(movement.Id, cancellationToken))
            return Result.Failure("يوجد طلب عكس قيد الاعتماد لهذه الحركة.", "Conflict");

        Result<InventoryApprovalRequest> created = InventoryApprovalRequest.CreateMovementReversal(
            movement.Id,
            request.Reason,
            userId,
            clock.GetUtcNow().UtcDateTime);
        if (!created.IsSuccess)
            return Result.Failure(created.ErrorMessage);

        approvals.Add(created.Value);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PersistenceConflictException)
        {
            return Result.Failure("يوجد طلب عكس قيد الاعتماد لهذه الحركة.", "Conflict");
        }
        catch (PersistenceException)
        {
            return Result.Failure("تعذر إرسال طلب العكس. حاول مرة أخرى.", "Persistence");
        }

        return Result.Success();
    }
}
