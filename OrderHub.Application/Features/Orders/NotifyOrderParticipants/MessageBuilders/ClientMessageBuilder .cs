using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Text;

namespace OrderHub.Application.Features.Orders.NotifyOrderParticipants.MessageBuilders;

internal sealed class ClientMessageBuilder : MessageBuilderBase, IRecipientMessageBuilder
{
    public OutboxMessage Build(OrderNotification order)
    {
        StringBuilder sb = new();

        string title = GetDisplayTitle(
            order,
            RecipientType.Client,
            order.Client.Id);

        CreateMessageHeader(sb, title, order);

        AppendProducts(
            sb,
            order,
            includePrice: true);

        sb.AppendLine("----------------------");
        sb.AppendLine($"الإجمالي: {order.Total:0.00}");

        AppendFooter(sb);

        return new OutboxMessage
        {
            OrderId = order.Id,
            RecipientType = RecipientType.Client,
            Text = sb.ToString(),
            Status = OutboxMessageStatus.Pending,

            Recipient = new ClientRecipient
            {
                ClientId = order.Client.Id,
                Name = order.Client.Name,
                PhoneNumber = order.Client.Phone
            }
        };
    }
}