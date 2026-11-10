#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Setup.Clients.Contracts;
using OrderHub.Application.Features.Setup.Deliverymen.Contracts;
using OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;
using OrderHub.Application.Features.Setup.Suppliers.Contracts;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models.Trips;

namespace OrderHub.Application.Features.Trips.CreateTrip;

internal sealed class CreateTripCommandHandler(
    IStandaloneTripRepository repository,
    IDeliverymanRepository deliverymanRepository,
    IClientRepository clientRepository,
    ISupplierRepository supplierRepository,
    IShippingCarrierRepository carrierRepository,
    INumberSequenceService numberSequenceService,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<CreateTripCommand, Result<CreateTripReceipt>>
{
    public async Task<Result<CreateTripReceipt>> Handle(
        CreateTripCommand request,
        CancellationToken cancellationToken)
    {
        var operationResult = TripOperationFactory.Create(currentUser, timeProvider);
        if (!operationResult.IsSuccess)
        {
            return Result<CreateTripReceipt>.Failure(operationResult.ErrorMessage, operationResult.ErrorCode);
        }

        var validation = TripInputValidation.ValidateCreate(
            request.Title,
            request.Description,
            request.From,
            request.To);

        if (!validation.IsSuccess)
        {
            return Result<CreateTripReceipt>.Failure(validation.ErrorMessage, "InvalidOperation");
        }

        var deliveryman = await deliverymanRepository.FindAsync(request.DeliverymanId, cancellationToken);
        if (deliveryman is null)
        {
            return Result<CreateTripReceipt>.Failure("المندوب غير موجود.", "NotFound");
        }

        var fromResult = await TripPartySnapshotFactory.ResolveAsync(
            request.From,
            clientRepository,
            supplierRepository,
            carrierRepository,
            cancellationToken);

        if (!fromResult.IsSuccess)
        {
            return Result<CreateTripReceipt>.Failure(fromResult.ErrorMessage, "InvalidOperation");
        }

        var toResult = await TripPartySnapshotFactory.ResolveAsync(
            request.To,
            clientRepository,
            supplierRepository,
            carrierRepository,
            cancellationToken);

        if (!toResult.IsSuccess)
        {
            return Result<CreateTripReceipt>.Failure(toResult.ErrorMessage, "InvalidOperation");
        }

        var operation = operationResult.Value;

        StandaloneTrip trip;
        try
        {
            trip = StandaloneTrip.Create(
                request.DeliverymanId,
                request.Title,
                request.Description,
                fromResult.Value,
                toResult.Value,
                request.ScheduledAtUtc,
                operation);

            trip.UpdateCreationDate(operation.AtUtc);
        }
        catch (InvalidOperationException exception)
        {
            return Result<CreateTripReceipt>.Failure(exception.Message, "InvalidOperation");
        }

        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);

            var allocation = await numberSequenceService.NextMonthlyAsync(
                NumberSequenceKind.Deliveryman,
                request.DeliverymanId,
                new DateTimeOffset(DateTime.SpecifyKind(trip.CreatedAt, DateTimeKind.Utc)),
                cancellationToken);

            trip.AssignNumber(allocation.Number, allocation.Year, allocation.Month);

            repository.Add(trip);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            return Result<CreateTripReceipt>.Success(new CreateTripReceipt(
                trip.Id,
                trip.Version,
                trip.Status,
                trip.TripNumber,
                trip.SequenceYear,
                trip.SequenceMonth));
        }
        catch (PersistenceConflictException)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            return Result<CreateTripReceipt>.Failure(
                "حدث تعارض أثناء الحفظ. يرجى المحاولة مرة أخرى.",
                "Conflict");
        }
        catch (PersistenceException exception)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            return Result<CreateTripReceipt>.Failure(
                $"خطأ في حفظ البيانات: {exception.Message}",
                "Persistence");
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
