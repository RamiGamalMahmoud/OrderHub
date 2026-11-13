using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Inventory.Counts.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.Counts.CancelCount;

internal sealed class CancelCountCommandHandler(
    IInventoryCountRepository counts,
    IAuthorizationService authorization,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock) : IRequestHandler<CancelCountCommand, Result<CountDecisionResult>>
{
    public async Task<Result<CountDecisionResult>> Handle(
        CancelCountCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result<CountDecisionResult>.Failure("يجب تسجيل الدخول لإلغاء الجرد.", "Unauthorized");

        InventoryCount count = await counts.GetForUpdateAsync(request.CountId, cancellationToken);
        if (count is null)
            return Result<CountDecisionResult>.Failure("الجرد غير موجود.", "NotFound");

        bool isKeeper = count.Warehouse?.KeeperUserId == userId;
        if (!isKeeper && !currentUser.IsAdmin)
        {
            bool canApprove = await authorization.IsAllowedAsync(
                PermissionFeature.Inventory,
                PermissionLevel.Approve,
                cancellationToken);
            if (!canApprove)
            {
                return Result<CountDecisionResult>.Failure(
                    "إلغاء الجرد متاح لأمين المخزن أو المدير المختص.",
                    "Forbidden");
            }
        }

        DateTime now = clock.GetUtcNow().UtcDateTime;
        Result cancelled = count.Cancel(userId, request.Note, now);
        if (!cancelled.IsSuccess)
            return Result<CountDecisionResult>.Failure(cancelled.ErrorMessage, cancelled.ErrorCode);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<CountDecisionResult>.Success(
                new CountDecisionResult(count.Id, count.Version, count.Status));
        }
        catch (PersistenceConflictException)
        {
            return Result<CountDecisionResult>.Failure(
                "تغيّر الجرد أثناء الإلغاء؛ حدّث الصفحة وأعد المحاولة.",
                "Conflict");
        }
        catch (PersistenceException)
        {
            return Result<CountDecisionResult>.Failure(
                "تعذر إلغاء الجرد. يرجى المحاولة مرة أخرى.",
                "Persistence");
        }
    }
}
