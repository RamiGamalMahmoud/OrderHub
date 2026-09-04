using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Linq;
using System.Text;

namespace OrderHub.Infrastructure.CommandHandlers.Orders.MessageBulders;

internal class ShippingCarrierMessageBuilder : MessageBuilderBase, IMessageBuilder
{
    public OutboxMessage Create(Order order, int recipientId, string recipientName, string contact)
    {
        return new OutboxMessage
        {
            Order = order,
            OrderId = order.Id,
            RecipientType = RecipientType.ShippingCarrier,
            Text = CreateMessageText(order, recipientId),
            Status = OutboxMessageStatus.Pending,
            Recipient = new ShippingCarrierRecipient
            {
                ShippingCarrierId = recipientId,
                Name = recipientName,
                PhoneNumber = contact
            }
        };
    }

    public string CreateMessageText(Order order, int destinationId)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"*{order.GetDisplayTitle(RecipientType.Client, order.ClientId)}*");
        sb.AppendLine();
        BuildBaseDetails(sb, order);
        sb.AppendLine("-----------------------------");
        sb.AppendLine("*المنتجات*");
        sb.AppendLine();

        int index = 1;
        foreach (OrderItem item in order.OrderItems)
        {
            AppendProductBlock(sb, index, item, includePrice: false);
            index++;
        }

        var currentStep = order.DeliverySteps
            .Single(x => x.ShippingCarrierId == destinationId);

        var nextStep = order.DeliverySteps
            .Where(x => x.StepOrder > currentStep.StepOrder)
            .OrderBy(x => x.StepOrder)
            .FirstOrDefault();
        AppendDeliveryDestination(sb, order, nextStep);
        AppendFooter(sb);

        return sb.ToString();
    }
}
