namespace OrderHub.UI.Messaging;

public static class DeliverymenMessages
{
    // The existing create command returns success only; listeners reload their lists.
    public sealed record DeliverymanCreatedMessage;

    public sealed record DeliverymanUpdatedMessage(int DeliverymanId);

    public sealed record DeliverymanDeletedMessage(int DeliverymanId);
}
