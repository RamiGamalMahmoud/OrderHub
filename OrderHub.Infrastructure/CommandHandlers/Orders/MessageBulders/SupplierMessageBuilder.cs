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
        StringBuilder sb = new StringBuilder();

        CreateMessageHeader(
            sb,
            order.GetDisplayTitle(RecipientType.Supplier, supplierId),
            order.OrderNumber,
            order);

        sb.AppendLine();
        sb.AppendLine("-----------------------------");
        sb.AppendLine("*المنتجات*");
        sb.AppendLine();

        int index = 1;
        IEnumerable<OrderItem> supplierItems = order.OrderItems.Where(i => i.SupplierId == supplierId);
        foreach (OrderItem item in supplierItems)
        {
            AppendProductBlock(sb, index, item, includePrice: false);
            index++;
        }

        AppendFooter(sb);

        return sb.ToString();
    }
}
