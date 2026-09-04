using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Linq;
using System.Text;

namespace OrderHub.Application.Features.Orders.NotifyOrderParticipants.MessageBuilders;

internal sealed class SupplierMessageBuilder : MessageBuilderBase, IRecipientMessageBuilder
{
    private readonly SupplierNotification _supplier;

    public SupplierMessageBuilder(SupplierNotification supplier)
    {
        _supplier = supplier;
    }

    public OutboxMessage Build(OrderNotification order)
    {
        StringBuilder sb = new();

        string title = GetDisplayTitle(
            order,
            RecipientType.Supplier,
            _supplier.Id);

        CreateMessageHeader(sb, title, order);

        sb.AppendLine($"*المورد:* {_supplier.Name}");
        sb.AppendLine();

        sb.AppendLine("-----------------------------");
        sb.AppendLine("*المنتجات*");
        sb.AppendLine();

        int index = 1;

        foreach (OrderItemNotification item in order.Items
            .Where(x => x.Supplier?.Id == _supplier.Id))
        {
            AppendProductBlock(
                sb,
                index,
                item,
                includePrice: false);

            index++;
        }

        AppendFooter(sb);

        return new OutboxMessage
        {
            OrderId = order.Id,
            RecipientType = RecipientType.Supplier,
            Text = sb.ToString(),
            Status = OutboxMessageStatus.Pending,

            Recipient = new SupplierRecipient
            {
                SupplierId = _supplier.Id,
                Name = _supplier.Name,
                PhoneNumber = _supplier.WhatsappGroupLink
            }
        };
    }
}