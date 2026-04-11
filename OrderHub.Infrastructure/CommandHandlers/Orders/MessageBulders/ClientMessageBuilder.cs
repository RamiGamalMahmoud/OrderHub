using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.CommandHandlers.Orders.MessageBulders;

internal class ClientMessageBuilder : MessageBuilderBase, IMessageBuilder
{
    public OutboxMessage Create(Order order, int recipientId, string recipientName, string contact)
    {
        return new OutboxMessage
        {
            Order = order,
            OrderId = order.Id,
            RecipientType = RecipientType.Client,
            Text = CreateMessageText(order),
            Status = OutboxMessageStatus.Pending,
            Recipient = new ClientRecipient
            {
                ClientId = recipientId,
                Name = recipientName,
                PhoneNumber = contact,
            }
        };
    }

    public string CreateMessageText(Order order)
    {
        CreateMessageHeader(
            order.GetDisplayTitle(RecipientType.Client, order.ClientId),
            order.OrderNumber,
            order);

        _sb.AppendLine("-----------------------------");
        _sb.AppendLine("*المنتجات*");
        _sb.AppendLine();

        int index = 1;
        foreach (OrderItem item in order.OrderItems)
        {
            AppendProductBlock(index, item, includePrice: true);
            index++;
        }

        _sb.AppendLine("----------------------");
        _sb.AppendLine($"الإجمالي: {order.Total.Value:0.00}");
        _sb.AppendLine();
        _sb.AppendLine("----------------------");
        _sb.AppendLine("> ملاحظة : ");

        return _sb.ToString();
    }
}
