namespace OrderHub.UI.Messaging;

public static class ClientsMessages
{
    // The existing create command returns success only; listeners reload their lists.
    public sealed record ClientCreatedMessage;

    public sealed record ClientUpdatedMessage(int ClientId);

    public sealed record ClientDeletedMessage(int ClientId);
}
