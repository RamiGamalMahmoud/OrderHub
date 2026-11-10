using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Application.Features.Trips.Lifecycle;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Trips.FailTrip;

internal sealed class FailTripCommandHandler(
    IStandaloneTripRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : TripLifecycleHandlerBase(repository, unitOfWork, currentUser, timeProvider),
      IRequestHandler<FailTripCommand, Result<int>>
{
    public Task<Result<int>> Handle(FailTripCommand request, CancellationToken cancellationToken)
    {
        var validation = ValidateReason(request.Reason);
        if (validation is not null)
        {
            return Task.FromResult(validation);
        }

        return ExecuteAsync(
            request.Id,
            request.ExpectedVersion,
            (trip, operation) => trip.Fail(request.Reason, operation),
            cancellationToken);
    }
}
