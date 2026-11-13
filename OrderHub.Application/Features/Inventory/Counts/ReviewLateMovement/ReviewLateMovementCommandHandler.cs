using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Inventory.Counts.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.Counts.ReviewLateMovement;

internal sealed class ReviewLateMovementCommandHandler(
    IInventoryCountRepository counts,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock) : IRequestHandler<ReviewLateMovementCommand, Result<CountDecisionResult>>
{
    public async Task<Result<CountDecisionResult>> Handle(
        ReviewLateMovementCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result<CountDecisionResult>.Failure("يجب تسجيل الدخول لمراجعة الجرد.", "Unauthorized");

        InventoryCount count = await counts.GetForUpdateAsync(request.CountId, cancellationToken);
        if (count is null)
            return Result<CountDecisionResult>.Failure("الجرد غير موجود.", "NotFound");

        if (count.Status != InventoryCountStatus.UnderReview)
        {
            return Result<CountDecisionResult>.Failure(
                "مراجعة الحركات المتأخرة متاحة بعد إنهاء العدّ فقط.",
                "Conflict");
        }

        InventoryCountLine line = count.Lines.FirstOrDefault(candidate =>
            candidate.Id == request.LineId && candidate.IsActive);
        if (line is null)
            return Result<CountDecisionResult>.Failure("سطر الجرد غير موجود.", "NotFound");

        IReadOnlyList<int> lateLineIds = await counts.GetLateMovementLineIdsAsync(count, cancellationToken);
        if (!lateLineIds.Contains(line.Id))
        {
            return Result<CountDecisionResult>.Failure(
                "لا توجد حركة متأخرة على هذا الصنف.",
                "Conflict");
        }

        DateTime now = clock.GetUtcNow().UtcDateTime;
        Result reviewed;
        if (request.Decision == LateMovementDecision.IncludedInCount)
        {
            BookQuantities book = await counts.GetBookQuantitiesAsync(
                count.WarehouseId,
                line.ProductId,
                line.SnapshotAtUtc,
                cancellationToken);
            reviewed = count.IncludeLateMovement(
                line.Id,
                book.SoundQty,
                book.DamagedQty,
                request.Note,
                now);
        }
        else if (request.Decision == LateMovementDecision.Recounted)
        {
            BookQuantities book = await counts.GetBookQuantitiesAsync(
                count.WarehouseId,
                line.ProductId,
                now,
                cancellationToken);
            reviewed = count.Recount(
                line.Id,
                book.SoundQty,
                book.DamagedQty,
                request.Note,
                now);
        }
        else
        {
            return Result<CountDecisionResult>.Failure("قرار المراجعة غير صالح.");
        }

        if (!reviewed.IsSuccess)
            return Result<CountDecisionResult>.Failure(reviewed.ErrorMessage, reviewed.ErrorCode);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<CountDecisionResult>.Success(
                new CountDecisionResult(count.Id, count.Version, count.Status));
        }
        catch (PersistenceConflictException)
        {
            return Result<CountDecisionResult>.Failure(
                "تغيّر الجرد أثناء المراجعة؛ حدّث الصفحة وأعد المحاولة.",
                "Conflict");
        }
        catch (PersistenceException)
        {
            return Result<CountDecisionResult>.Failure(
                "تعذر تسجيل قرار المراجعة. يرجى المحاولة مرة أخرى.",
                "Persistence");
        }
    }
}
