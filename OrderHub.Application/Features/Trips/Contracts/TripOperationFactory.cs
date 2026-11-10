using System;
using OrderHub.Application.Context;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models.Trips;

namespace OrderHub.Application.Features.Trips.Contracts;

public static class TripOperationFactory
{
    public static Result<TripOperation> Create(ICurrentUser currentUser, TimeProvider timeProvider)
    {
        if (!currentUser.IsAuthenticated || currentUser.Id is not > 0)
        {
            return Result<TripOperation>.Failure("يجب تسجيل الدخول لتنفيذ العملية.", "Unauthorized");
        }

        var userName = (currentUser.DisplayName ?? currentUser.UserName)?.Trim();
        if (string.IsNullOrWhiteSpace(userName))
        {
            return Result<TripOperation>.Failure("تعذر تحديد المستخدم الحالي.", "Unauthorized");
        }

        var atUtc = DateTime.SpecifyKind(timeProvider.GetUtcNow().UtcDateTime, DateTimeKind.Utc);
        return new TripOperation(currentUser.Id.Value, userName, atUtc);
    }
}
