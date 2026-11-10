using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Application.Features.Trips.Lifecycle;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Trips.RetryTrip;

internal sealed class RetryTripCommandHandler(
    IStandaloneTripRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : TripLifecycleHandlerBase(repository, unitOfWork, currentUser, timeProvider),
      IRequestHandler<RetryTripCommand, Result<int>>
{
    public Task<Result<int>> Handle(RetryTripCommand request, CancellationToken cancellationToken) =>
        ExecuteAsync(
            request.Id,
            request.ExpectedVersion,
            (trip, operation) => trip.Retry(operation),
            cancellationToken);
}
