using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Text;

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
        StringBuilder sb = new StringBuilder();

        CreateMessageHeader(
            sb,
            order.GetDisplayTitle(RecipientType.Client, order.ClientId),
            order.OrderNumber,
            order);

        sb.AppendLine("-----------------------------");
        sb.AppendLine("*المنتجات*");
        sb.AppendLine();

        int index = 1;
        foreach (OrderItem item in order.OrderItems)
        {
            AppendProductBlock(sb, index, item, includePrice: true);
            index++;
        }

        sb.AppendLine("----------------------");
        sb.AppendLine($"الإجمالي: {order.Total.Value:0.00}");
        AppendFooter(sb);

        return sb.ToString();
    }
}
