namespace OrderHub.UI.Messaging;

public static class CategoriesMessages
{
    // The existing create command returns success only; listeners reload their lists.
    public sealed record CategoryCreatedMessage;

    public sealed record CategoryUpdatedMessage(int CategoryId);

    public sealed record CategoryDeletedMessage(int CategoryId);
}
