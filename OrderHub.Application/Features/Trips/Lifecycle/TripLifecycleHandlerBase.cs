#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models.Trips;

namespace OrderHub.Application.Features.Trips.Lifecycle;

internal abstract class TripLifecycleHandlerBase(
    IStandaloneTripRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    protected const string ConcurrencyMessage =
        "تم تعديل المشوار من مستخدم آخر. أعد تحميل البيانات ثم حاول مرة أخرى.";

    protected static Result<int>? ValidateReason(string? reason)
    {
        var trimmed = reason?.Trim();
        return string.IsNullOrEmpty(trimmed) || trimmed.Length > 200
            ? Result<int>.Failure("السبب مطلوب ويجب ألا يتجاوز 200 حرف.", "InvalidOperation")
            : null;
    }

    protected async Task<Result<int>> ExecuteAsync(
        int id,
        int expectedVersion,
        Action<StandaloneTrip, TripOperation> change,
        CancellationToken cancellationToken)
    {
        var operationResult = TripOperationFactory.Create(currentUser, timeProvider);
        if (!operationResult.IsSuccess)
        {
            return Result<int>.Failure(operationResult.ErrorMessage, operationResult.ErrorCode);
        }

        var trip = await repository.GetForUpdateAsync(id, cancellationToken);
        if (trip is null)
        {
            return Result<int>.Failure("المشوار غير موجود.", "NotFound");
        }

        if (trip.Version != expectedVersion)
        {
            return Result<int>.Failure(ConcurrencyMessage, "Conflict");
        }

        try
        {
            change(trip, operationResult.Value);
        }
        catch (InvalidOperationException exception)
        {
            return Result<int>.Failure(exception.Message, "InvalidOperation");
        }
        catch (ArgumentException exception)
        {
            return Result<int>.Failure(exception.Message, "InvalidOperation");
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<int>.Success(trip.Version);
        }
        catch (PersistenceConflictException)
        {
            return Result<int>.Failure(ConcurrencyMessage, "Conflict");
        }
        catch (PersistenceException)
        {
            return Result<int>.Failure("تعذر حفظ العملية. لم يتم اعتماد أي تغيير.", "Persistence");
        }
    }
}
