using MediatR;
using OrderHub.Application.Features.Orders.NotifyOrderParticipants.MessageBuilders;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Stores;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.NotifyOrderParticipants;

internal sealed class NotifyOrderParticipantsCommandHandler(
    IOrderNotificationQuery orderNotificationQuery,
    IOutboxMessageRepository outboxMessageRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<NotifyOrderParticipantsCommand, Result>
{
    public async Task<Result> Handle(
        NotifyOrderParticipantsCommand request,
        CancellationToken cancellationToken)
    {
        OrderNotification order =
            await orderNotificationQuery.GetForNotificationAsync(
                request.OrderId,
                cancellationToken);

        if (order is null)
            return Result.Failure("الطلب غير موجود.");

        List<IRecipientMessageBuilder> builders = 

        CreateRecipientBuilders(order, request.Recipient).ToList();

        if (builders.Count == 0)
            return Result.Success();
        IEnumerable<OutboxMessage> messages = builders.Select(builder => builder.Build(order));
        outboxMessageRepository.AddRange(messages);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception)
        {
            return Result.Failure(
                "Failed to create order notification messages.");
        }
    }

    private static IEnumerable<IRecipientMessageBuilder> CreateRecipientBuilders(
        OrderNotification order,
        NotificationRecipient recipient)
    {
        if (recipient is null || recipient.Type is RecipientType.Client)
        {
            yield return new ClientMessageBuilder();
        }

        if (recipient is null || recipient.Type is RecipientType.Supplier)
        {
            foreach (var supplier in CreateSupplierMessages(order))
            {
                yield return supplier;
            }
        }

        if (recipient is null || recipient.Type is
            RecipientType.Deliveryman or
            RecipientType.ShippingCarrier)
        {
            foreach (var dlivery in CreateDeliveryMessages(order, recipient?.Type))
            {
                yield return dlivery;
            }
        }
    }

    private static IEnumerable<IRecipientMessageBuilder> CreateSupplierMessages(
        OrderNotification order)
    {
        return order.Items
            .Where(item => item.Supplier is not null)
            .GroupBy(item => item.Supplier.Id)
            .Select(group =>
            {
                SupplierNotification supplier =
                    group.First().Supplier;

                return new SupplierMessageBuilder(supplier);
            });
    }

    private static IEnumerable<IRecipientMessageBuilder> CreateDeliveryMessages(
        OrderNotification order,
        RecipientType? recipientType)
    {
        if (order.DeliveryMethod == DeliveryMethod.DeliveryChain)
        {
            return CreateDeliveryChainMessages(
                order,
                recipientType);
        }

        return CreateSingleDeliveryMessages(
            order,
            recipientType);
    }

    private static IEnumerable<IRecipientMessageBuilder> CreateSingleDeliveryMessages(
        OrderNotification order,
        RecipientType? recipientType)
    {
        List<IRecipientMessageBuilder> builders = [];

        if (order.Deliveryman is not null &&
            (recipientType is null ||
             recipientType == RecipientType.Deliveryman))
        {
            builders.Add(new DeliverymanMessageBuilder(order.Deliveryman, null));
        }

        if (order.ShippingCarrier is not null &&
            (recipientType is null ||
             recipientType == RecipientType.ShippingCarrier))
        {
            builders.Add(new ShippingCarrierMessageBuilder(order.ShippingCarrier, null));
        }

        return builders;
    }

    private static IEnumerable<IRecipientMessageBuilder> CreateDeliveryChainMessages(
        OrderNotification order,
        RecipientType? recipientType)
    {
        List<DeliveryStepNotification> steps = order.DeliverySteps
            .OrderBy(step => step.StepOrder)
            .ToList();

        List<IRecipientMessageBuilder> builders = [];

        for (int index = 0; index < steps.Count; index++)
        {
            DeliveryStepNotification currentStep = steps[index];

            DeliveryStepNotification nextStep =
                index + 1 < steps.Count
                    ? steps[index + 1]
                    : null;

            if (currentStep.DeliveryMethod == DeliveryMethod.DeliveryMan &&
                (recipientType is null ||
                 recipientType == RecipientType.Deliveryman))
            {
                if (currentStep.Deliveryman is not null)
                {
                    builders.Add(new DeliverymanMessageBuilder(currentStep.Deliveryman, nextStep));
                }
            }

            if (currentStep.DeliveryMethod == DeliveryMethod.ShippingCompany &&
                (recipientType is null ||
                 recipientType == RecipientType.ShippingCarrier))
            {
                if (currentStep.ShippingCarrier is not null)
                {
                    builders.Add(
                        new ShippingCarrierMessageBuilder(
                            currentStep.ShippingCarrier,
                            nextStep));
                }
            }
        }

        return builders;
    }
}