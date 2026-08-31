using OrderHub.Domain.Enums;

namespace OrderHub.Application.Messages.OutboxMessages
{
    public record MessageStatusChangedMessage(int Id, OutboxMessageStatus NewStatus, int? OrderId, RecipientType RecipientType);
}
