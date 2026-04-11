using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OrderHub.Infrastructure.CommandHandlers.Orders.MessageBulders;

internal class SupplierMessageBuilder : MessageBuilderBase, IMessageBuilder
{
    public OutboxMessage Create(Order order, int recipientId, string recipientName, string contact)
    {
        return new OutboxMessage
        {
            Order = order,
            OrderId = order.Id,
            RecipientType = RecipientType.Supplier,
            Text = CreateMessageText(order, recipientId),
            Status = OutboxMessageStatus.Pending,
            Recipient = new SupplierRecipient
            {
                SupplierId = recipientId,
                Name = recipientName,
                PhoneNumber = contact
            }
        };
    }

    public string CreateMessageText(Order order, int supplierId)
    {
        CreateMessageHeader(
            order.GetDisplayTitle(RecipientType.Supplier, supplierId),
            order.OrderNumber,
            order);

        _sb.AppendLine();
        _sb.AppendLine("-----------------------------");
        _sb.AppendLine("*المنتجات*");
        _sb.AppendLine();

        int index = 1;
        IEnumerable<OrderItem> supplierItems = order.OrderItems.Where(i => i.SupplierId == supplierId);
        foreach (OrderItem item in supplierItems)
        {
            AppendProductBlock(index, item, includePrice: false);
            index++;
        }

        _sb.AppendLine("-----------------------------");
        _sb.AppendLine("> ملاحظة : ");

        return _sb.ToString();
    }
}
