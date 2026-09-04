using OrderHub.Domain.Models;

namespace OrderHub.Application.Features.Orders.NotifyOrderParticipants.MessageBuilders;

internal interface IRecipientMessageBuilder
{
    OutboxMessage Build(OrderNotification order);
}
