using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Application.Features.Trips.Lifecycle;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Trips.CompleteTrip;

internal sealed class CompleteTripCommandHandler(
    IStandaloneTripRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : TripLifecycleHandlerBase(repository, unitOfWork, currentUser, timeProvider),
      IRequestHandler<CompleteTripCommand, Result<int>>
{
    public Task<Result<int>> Handle(CompleteTripCommand request, CancellationToken cancellationToken) =>
        ExecuteAsync(
            request.Id,
            request.ExpectedVersion,
            (trip, operation) => trip.Complete(operation),
            cancellationToken);
}
