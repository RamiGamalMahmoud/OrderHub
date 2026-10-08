namespace OrderHub.UI.Messaging;

public static class WhatsAppGroupsMessages
{
    // The existing create command returns success only; listeners reload their lists.
    public sealed record WhatsappGroupCreatedMessage;

    public sealed record WhatsappGroupUpdatedMessage(int WhatsappGroupId);

    public sealed record WhatsappGroupDeletedMessage(int WhatsappGroupId);
}
