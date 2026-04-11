using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.CommandHandlers.Orders.MessageBulders;

internal class DeliverymanMessageBuilder : MessageBuilderBase, IMessageBuilder
{
    public OutboxMessage Create(Order order, int recipientId, string recipientName, string contact)
    {
        return new OutboxMessage
        {
            Order = order,
            OrderId = order.Id,
            RecipientType = RecipientType.Deliveryman,
            Text = CreateMessageText(order, recipientId, recipientName),
            Status = OutboxMessageStatus.Pending,
            Recipient = new DeliverymanRecipient
            {
                DeliveryManId = recipientId,
                Name = recipientName,
                PhoneNumber = contact
            }
        };
    }

    public string CreateMessageText(Order order, int deliverymanId, string deliverymanName)
    {
        _sb.AppendLine($"*{order.GetDisplayTitle(RecipientType.Deliveryman, deliverymanId)}*");
        _sb.AppendLine();
        BuildBaseDetails(order);
        _sb.AppendLine($"*اسم المندوب:* {deliverymanName}");
        _sb.AppendLine("-----------------------------");
        _sb.AppendLine("*المنتجات*");
        _sb.AppendLine();

        int index = 1;
        foreach (OrderItem item in order.OrderItems)
        {
            AppendProductBlock(index, item, includePrice: false);
            index++;
        }

        _sb.AppendLine("----------------------");
        _sb.AppendLine(" ");
        _sb.AppendLine("> ملاحظة : ");

        return _sb.ToString();
    }
}
