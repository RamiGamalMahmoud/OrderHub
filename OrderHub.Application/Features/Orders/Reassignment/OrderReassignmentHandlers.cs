using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Orders.Lifecycle;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;

namespace OrderHub.Application.Features.Orders.Reassignment;

internal sealed class OrderReassignmentHandlers(IOrderRepository repository, IUnitOfWork unitOfWork,
    ICurrentUser user, TimeProvider clock) :
    IRequestHandler<ReassignDeliveryStepCommand, Result<OrderOperationReceipt>>,
    IRequestHandler<CancelDeliveryStepCommand, Result<OrderOperationReceipt>>,
    IRequestHandler<ReassignDirectDeliveryCommand, Result<OrderOperationReceipt>>
{
    public async Task<Result<OrderOperationReceipt>> Handle(ReassignDeliveryStepCommand request,
        CancellationToken cancellationToken)
    {
        var (order, failure) = await LoadAsync(request.OrderId, request.ExpectedVersion, cancellationToken);
        if (failure is not null) return failure;
        var step = order.DeliverySteps.SingleOrDefault(item => item.Id == request.StepId);
        if (step is null) return Failure("خطوة التوصيل غير موجودة في الطلب.");
        var target = await ResolveTargetAsync(request.Method, request.PartyId, cancellationToken);
        if (!target.IsSuccess) return Failure(target.ErrorMessage, target.ErrorCode ?? "InvalidOperation");
        var changed = order.ReassignDeliveryStep(request.StepId, request.Method, target.Value,
            PreviousStepPartyName(step), Operation(), request.Reason);
        if (!changed.IsSuccess) return Failure(changed.ErrorMessage);
        return await PersistAsync(order, async token =>
        {
            await repository.TombstoneDeliveryStepAsync(changed.Value.CancelledStep, token);
            await unitOfWork.SaveChangesAsync(token);
        }, cancellationToken);
    }

    public async Task<Result<OrderOperationReceipt>> Handle(CancelDeliveryStepCommand request,
        CancellationToken cancellationToken)
    {
        var (order, failure) = await LoadAsync(request.OrderId, request.ExpectedVersion, cancellationToken);
        if (failure is not null) return failure;
        var step = order.DeliverySteps.SingleOrDefault(item => item.Id == request.StepId);
        if (step is null) return Failure("خطوة التوصيل غير موجودة في الطلب.");
        var changed = order.CancelDeliveryStep(request.StepId, PreviousStepPartyName(step),
            Operation(), request.Reason);
        if (!changed.IsSuccess) return Failure(changed.ErrorMessage);
        return await PersistAsync(order, async token =>
        {
            await repository.TombstoneDeliveryStepAsync(changed.Value, token);
            await unitOfWork.SaveChangesAsync(token);
        }, cancellationToken);
    }

    public async Task<Result<OrderOperationReceipt>> Handle(ReassignDirectDeliveryCommand request,
        CancellationToken cancellationToken)
    {
        var (order, failure) = await LoadAsync(request.OrderId, request.ExpectedVersion, cancellationToken);
        if (failure is not null) return failure;
        var target = await ResolveTargetAsync(request.Method, request.PartyId, cancellationToken);
        if (!target.IsSuccess) return Failure(target.ErrorMessage, target.ErrorCode ?? "InvalidOperation");
        var changed = order.ReassignDirectDelivery(request.Method, target.Value,
            PreviousDirectPartyName(order), Operation(), request.Reason);
        if (!changed.IsSuccess) return Failure(changed.ErrorMessage);
        return await PersistAsync(order, unitOfWork.SaveChangesAsync, cancellationToken);
    }

    private async Task<(Order Order, Result<OrderOperationReceipt> Failure)> LoadAsync(int orderId,
        int expectedVersion, CancellationToken cancellationToken)
    {
        if (!user.IsAuthenticated || user.Id is not > 0)
            return (null, Failure("يجب تسجيل الدخول لتنفيذ العملية.", "Unauthorized"));
        var order = await repository.GetForLifecycleAsync(orderId, cancellationToken);
        if (order is null) return (null, Failure("الطلب غير موجود.", "NotFound"));
        var valid = order.CheckVersion(expectedVersion);
        return valid.IsSuccess ? (order, null) : (null, Failure(valid.ErrorMessage, "Conflict"));
    }

    private async Task<Result<DeliveryParty>> ResolveTargetAsync(DeliveryMethod method, int? partyId,
        CancellationToken cancellationToken)
    {
        if (method == DeliveryMethod.Pickup) return Result<DeliveryParty>.Success(null);
        if (method is not (DeliveryMethod.DeliveryMan or DeliveryMethod.ShippingCompany))
            return Result<DeliveryParty>.Failure("طريقة التوصيل المطلوبة غير صالحة.");
        if (partyId is not > 0) return Result<DeliveryParty>.Failure("يجب تحديد جهة التوصيل.");
        var party = await repository.FindDeliveryPartyAsync(method, partyId.Value, cancellationToken);
        return party is null
            ? Result<DeliveryParty>.Failure("جهة التنفيذ المحددة غير موجودة.", "NotFound")
            : Result<DeliveryParty>.Success(party);
    }

    private async Task<Result<OrderOperationReceipt>> PersistAsync(Order order,
        Func<CancellationToken, Task> stage, CancellationToken cancellationToken)
    {
        var active = false;
        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);
            active = true;
            await stage(cancellationToken);
            active = false;
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            return new OrderOperationReceipt(order.Id, order.Version, order.OrderStatus);
        }
        catch (PersistenceConflictException exception)
        {
            if (active) await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            return Failure(exception.Message, "Conflict");
        }
        catch (PersistenceException)
        {
            if (active) await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            return Failure("تعذر حفظ العملية. لم يتم اعتماد أي تغيير.", "Persistence");
        }
    }

    private OrderOperation Operation() =>
        new(user.Id.Value, user.DisplayName ?? user.UserName, clock.GetUtcNow().UtcDateTime);

    private static string PreviousStepPartyName(OrderDeliveryStep step) => step.DeliveryMethod switch
    {
        DeliveryMethod.DeliveryMan => step.Deliveryman?.Name.Value,
        DeliveryMethod.ShippingCompany => step.ShippingCarrier?.Name.Value,
        _ => null
    };

    private static string PreviousDirectPartyName(Order order) => order.DeliveryMethod switch
    {
        DeliveryMethod.DeliveryMan => order.Deliveryman?.Name.Value,
        DeliveryMethod.ShippingCompany => order.ShippingCarrier?.Name.Value,
        _ => null
    };

    private static Result<OrderOperationReceipt> Failure(string message, string code = "InvalidOperation") =>
        Result<OrderOperationReceipt>.Failure(message, code);
}
