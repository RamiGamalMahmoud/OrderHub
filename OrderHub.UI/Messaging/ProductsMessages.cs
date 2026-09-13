namespace OrderHub.UI.Messaging;

public static class ProductsMessages
{
    public sealed record ProductCreatedMessage(int ProductId);

    public sealed record ProductUpdatedMessage(int ProductId);

    public sealed record ProductDeletedMessage(int ProductId);
}
