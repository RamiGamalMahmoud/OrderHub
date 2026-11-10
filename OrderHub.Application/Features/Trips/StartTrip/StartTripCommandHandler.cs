using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Application.Features.Trips.Lifecycle;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Trips.StartTrip;

internal sealed class StartTripCommandHandler(
    IStandaloneTripRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : TripLifecycleHandlerBase(repository, unitOfWork, currentUser, timeProvider),
      IRequestHandler<StartTripCommand, Result<int>>
{
    public Task<Result<int>> Handle(StartTripCommand request, CancellationToken cancellationToken) =>
        ExecuteAsync(
            request.Id,
            request.ExpectedVersion,
            (trip, operation) => trip.Start(operation),
            cancellationToken);
}
