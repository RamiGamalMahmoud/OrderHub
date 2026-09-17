namespace OrderHub.UI.Messaging;

public static class MessagesMessages
{
    public sealed record OutboxMessageDeletedMessage(int MessageId);

    // Queue commands do not return outbox IDs. OrderId identifies an order batch,
    // or is null for a manually composed message; listeners reload through a query.
    public sealed record OutboxMessagesQueuedMessage(int? OrderId);
}
