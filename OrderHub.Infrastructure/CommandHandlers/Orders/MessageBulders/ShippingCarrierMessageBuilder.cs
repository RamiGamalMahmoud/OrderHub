using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;

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
            Text = CreateMessageText(order),
            Status = OutboxMessageStatus.Pending,
            Recipient = new ShippingCarrierRecipient
            {
                ShippingCarrierId = recipientId,
                Name = recipientName,
                PhoneNumber = contact
            }
        };
    }

    public string CreateMessageText(Order order)
    {
        _sb.AppendLine($"*{order.GetDisplayTitle(RecipientType.Client, order.ClientId)}*");
        _sb.AppendLine();
        BuildBaseDetails(order);
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
