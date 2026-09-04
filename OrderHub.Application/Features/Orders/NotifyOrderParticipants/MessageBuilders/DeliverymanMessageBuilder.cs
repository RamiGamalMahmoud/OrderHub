using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Text;

namespace OrderHub.Application.Features.Orders.NotifyOrderParticipants.MessageBuilders;

internal sealed class DeliverymanMessageBuilder : MessageBuilderBase, IRecipientMessageBuilder
{
    private readonly DeliverymanNotification _deliveryman;
    private readonly DeliveryStepNotification _nextStep;

    public DeliverymanMessageBuilder(DeliverymanNotification deliveryman, DeliveryStepNotification nextStep)
    {
        _deliveryman = deliveryman;
        _nextStep = nextStep;
    }

    public OutboxMessage Build(OrderNotification order)
    {
        StringBuilder sb = new();

        string title = GetDisplayTitle(
            order,
            RecipientType.Deliveryman,
            _deliveryman.Id);

        CreateMessageHeader(sb, title, order);

        sb.AppendLine($"*اسم المندوب:* {_deliveryman.Name}");

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
            RecipientType = RecipientType.Deliveryman,
            Text = sb.ToString(),
            Status = OutboxMessageStatus.Pending,

            Recipient = new DeliverymanRecipient
            {
                DeliveryManId = _deliveryman.Id,
                Name = _deliveryman.Name,
                PhoneNumber = _deliveryman.WhatsappGroupLink
            }
        };
    }
}