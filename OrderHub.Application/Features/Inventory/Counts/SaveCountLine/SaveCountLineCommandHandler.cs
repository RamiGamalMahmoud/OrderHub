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

namespace OrderHub.Application.Features.Inventory.Counts.SaveCountLine;

internal sealed class SaveCountLineCommandHandler(
    IInventoryCountRepository counts,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock) : IRequestHandler<SaveCountLineCommand, Result>
{
    public async Task<Result> Handle(
        SaveCountLineCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result.Failure("يجب تسجيل الدخول لحفظ العدّ.", "Unauthorized");

        if (request.CountId <= 0)
            return Result.Failure("الجرد غير صالح.");

        InventoryCount count = await counts.GetForUpdateAsync(request.CountId, cancellationToken);
        if (count is null)
            return Result.Failure("الجرد غير موجود.", "NotFound");

        if (count.Warehouse?.KeeperUserId != userId && !currentUser.IsAdmin)
        {
            return Result.Failure(
                "تعديل الجرد متاح لأمين المخزن المرتبط بهذا المخزن فقط.",
                "Forbidden");
        }

        DateTime now = clock.GetUtcNow().UtcDateTime;
        Result saved = count.SaveCounted(
            request.LineId,
            request.CountedQty,
            request.CountedDamagedQty,
            now);
        if (!saved.IsSuccess)
            return saved;

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (PersistenceConflictException)
        {
            return Result.Failure("تغيّر الجرد أثناء الحفظ؛ حدّث الصفحة وأعد المحاولة.", "Conflict");
        }
        catch (PersistenceException)
        {
            return Result.Failure("تعذر حفظ العدّ. يرجى المحاولة مرة أخرى.", "Persistence");
        }
    }
}
