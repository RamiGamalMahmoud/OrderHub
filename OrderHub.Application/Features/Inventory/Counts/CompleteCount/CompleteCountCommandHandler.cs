using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Inventory.Counts.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.Counts.CompleteCount;

internal sealed class CompleteCountCommandHandler(
    IInventoryCountRepository counts,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock) : IRequestHandler<CompleteCountCommand, Result<CountDecisionResult>>
{
    public async Task<Result<CountDecisionResult>> Handle(
        CompleteCountCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result<CountDecisionResult>.Failure("يجب تسجيل الدخول لإنهاء الجرد.", "Unauthorized");

        InventoryCount count = await counts.GetForUpdateAsync(request.CountId, cancellationToken);
        if (count is null)
            return Result<CountDecisionResult>.Failure("الجرد غير موجود.", "NotFound");

        if (count.Warehouse?.KeeperUserId != userId && !currentUser.IsAdmin)
        {
            return Result<CountDecisionResult>.Failure(
                "إنهاء الجرد متاح لأمين المخزن المرتبط بهذا المخزن فقط.",
                "Forbidden");
        }

        DateTime now = clock.GetUtcNow().UtcDateTime;
        Result completed = count.Complete(now);
        if (!completed.IsSuccess)
            return Result<CountDecisionResult>.Failure(completed.ErrorMessage, completed.ErrorCode);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<CountDecisionResult>.Success(
                new CountDecisionResult(count.Id, count.Version, count.Status));
        }
        catch (PersistenceConflictException)
        {
            return Result<CountDecisionResult>.Failure(
                "تغيّر الجرد أثناء الحفظ؛ حدّث الصفحة وأعد المحاولة.",
                "Conflict");
        }
        catch (PersistenceException)
        {
            return Result<CountDecisionResult>.Failure(
                "تعذر إنهاء الجرد. يرجى المحاولة مرة أخرى.",
                "Persistence");
        }
    }
}
