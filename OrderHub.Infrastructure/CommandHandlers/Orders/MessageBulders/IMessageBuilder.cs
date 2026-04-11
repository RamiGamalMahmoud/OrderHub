using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.CommandHandlers.Orders.MessageBulders;

internal interface IMessageBuilder
{
    OutboxMessage Create(Order order, int recipientId, string recipientName, string contact);
}
