using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Application.Features.Trips.Lifecycle;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Trips.CancelTrip;

internal sealed class CancelTripCommandHandler(
    IStandaloneTripRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : TripLifecycleHandlerBase(repository, unitOfWork, currentUser, timeProvider),
      IRequestHandler<CancelTripCommand, Result<int>>
{
    public Task<Result<int>> Handle(CancelTripCommand request, CancellationToken cancellationToken)
    {
        var validation = ValidateReason(request.Reason);
        if (validation is not null)
        {
            return Task.FromResult(validation);
        }

        return ExecuteAsync(
            request.Id,
            request.ExpectedVersion,
            (trip, operation) => trip.Cancel(request.Reason, operation),
            cancellationToken);
    }
}
