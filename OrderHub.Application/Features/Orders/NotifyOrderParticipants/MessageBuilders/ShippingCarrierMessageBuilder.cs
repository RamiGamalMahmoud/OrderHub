using OrderHub.Application.Features.Orders.NotifyOrderParticipants;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Text;

namespace OrderHub.Application.Features.Orders.NotifyOrderParticipants.MessageBuilders;

internal sealed class ShippingCarrierMessageBuilder : MessageBuilderBase, IRecipientMessageBuilder
{
    private readonly ShippingCarrierNotification _shippingCarrier;
    private readonly DeliveryStepNotification _nextStep;

    public ShippingCarrierMessageBuilder(
        ShippingCarrierNotification shippingCarrier,
        DeliveryStepNotification nextStep)
    {
        _shippingCarrier = shippingCarrier;
        _nextStep = nextStep;
    }

    public OutboxMessage Build(OrderNotification order)
    {
        StringBuilder sb = new();

        string title = GetDisplayTitle(
            order,
            RecipientType.ShippingCarrier,
            _shippingCarrier.Id);

        CreateMessageHeader(sb, title, order);

        sb.AppendLine($"*شركة الشحن:* {_shippingCarrier.Name}");

        AppendProducts(
            sb,
            order,
            includePrice: false);

        AppendDeliveryDestination(
            sb,
            order,
            _nextStep);

        AppendFooter(sb);

        return new OutboxMessage
        {
            OrderId = order.Id,
            RecipientType = RecipientType.ShippingCarrier,
            Text = sb.ToString(),
            Status = OutboxMessageStatus.Pending,

            Recipient = new ShippingCarrierRecipient
            {
                ShippingCarrierId = _shippingCarrier.Id,
                Name = _shippingCarrier.Name,
                PhoneNumber = _shippingCarrier.Phone
            }
        };
    }
}