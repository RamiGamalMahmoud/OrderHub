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

namespace OrderHub.Application.Features.Trips.UpdateTrip;

internal sealed class UpdateTripCommandHandler(
    IStandaloneTripRepository repository,
    IDeliverymanRepository deliverymanRepository,
    IClientRepository clientRepository,
    ISupplierRepository supplierRepository,
    IShippingCarrierRepository carrierRepository,
    INumberSequenceService numberSequenceService,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<UpdateTripCommand, Result<int>>
{
    private const string ConcurrencyMessage =
        "تم تعديل المشوار من مستخدم آخر. أعد تحميل البيانات ثم حاول مرة أخرى.";

    public async Task<Result<int>> Handle(
        UpdateTripCommand request,
        CancellationToken cancellationToken)
    {
        var operationResult = TripOperationFactory.Create(currentUser, timeProvider);
        if (!operationResult.IsSuccess)
        {
            return Result<int>.Failure(operationResult.ErrorMessage, operationResult.ErrorCode);
        }

        var trip = await repository.GetForEditAsync(request.Id, cancellationToken);
        if (trip is null)
        {
            return Result<int>.Failure("المشوار غير موجود.", "NotFound");
        }

        if (trip.Version != request.ExpectedVersion)
        {
            return Result<int>.Failure(ConcurrencyMessage, "Conflict");
        }

        if (!trip.IsNew)
        {
            return Result<int>.Failure(
                "لا يمكن تعديل المشوار بعد بدء المحاولة الأولى.",
                "InvalidOperation");
        }

        var validation = TripInputValidation.ValidateUpdate(
            request.Title,
            request.Description,
            request.From,
            request.To,
            trip.DeliverymanId,
            request.DeliverymanId,
            request.EditNote,
            request.ReassignReason);

        if (!validation.IsSuccess)
        {
            return Result<int>.Failure(validation.ErrorMessage, "InvalidOperation");
        }

        bool isReassignment = request.DeliverymanId != trip.DeliverymanId;
        string? newDeliverymanName = null;

        if (isReassignment)
        {
            var newDeliveryman = await deliverymanRepository.FindAsync(request.DeliverymanId, cancellationToken);
            if (newDeliveryman is null)
            {
                return Result<int>.Failure("المندوب غير موجود.", "NotFound");
            }

            newDeliverymanName = newDeliveryman.Name.Value;
        }

        var fromResult = await TripPartySnapshotFactory.ResolveAsync(
            request.From,
            clientRepository,
            supplierRepository,
            carrierRepository,
            cancellationToken);

        if (!fromResult.IsSuccess)
        {
            return Result<int>.Failure(fromResult.ErrorMessage, "InvalidOperation");
        }

        var toResult = await TripPartySnapshotFactory.ResolveAsync(
            request.To,
            clientRepository,
            supplierRepository,
            carrierRepository,
            cancellationToken);

        if (!toResult.IsSuccess)
        {
            return Result<int>.Failure(toResult.ErrorMessage, "InvalidOperation");
        }

        var operation = operationResult.Value;
        bool transactionStarted = false;

        try
        {
            long? newTripNumber = null;

            if (isReassignment)
            {
                await unitOfWork.BeginTransactionAsync(cancellationToken);
                transactionStarted = true;

                var allocation = await numberSequenceService.NextMonthlyAsync(
                    NumberSequenceKind.Deliveryman,
                    request.DeliverymanId,
                    new DateTimeOffset(DateTime.SpecifyKind(trip.CreatedAt, DateTimeKind.Utc)),
                    cancellationToken);

                newTripNumber = allocation.Number;
            }

            trip.Update(
                request.Title,
                request.Description,
                fromResult.Value,
                toResult.Value,
                request.ScheduledAtUtc,
                request.DeliverymanId,
                newDeliverymanName,
                newTripNumber,
                isReassignment ? request.ReassignReason : request.EditNote,
                operation);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            if (transactionStarted)
            {
                await unitOfWork.CommitTransactionAsync(cancellationToken);
            }

            return Result<int>.Success(trip.Version);
        }
        catch (InvalidOperationException exception)
        {
            if (transactionStarted)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
            }

            return Result<int>.Failure(exception.Message, "InvalidOperation");
        }
        catch (PersistenceConflictException)
        {
            if (transactionStarted)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
            }

            return Result<int>.Failure(ConcurrencyMessage, "Conflict");
        }
        catch (PersistenceException)
        {
            if (transactionStarted)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
            }

            return Result<int>.Failure("تعذر حفظ المشوار. لم يتم اعتماد أي تغيير.", "Persistence");
        }
        catch
        {
            if (transactionStarted)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
            }

            throw;
        }
    }
}
