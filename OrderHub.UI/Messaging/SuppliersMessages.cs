namespace OrderHub.UI.Messaging;

public static class SuppliersMessages
{
    // The existing create command returns success only; listeners reload their lists.
    public sealed record SupplierCreatedMessage;

    public sealed record SupplierUpdatedMessage(int SupplierId);

    public sealed record SupplierDeletedMessage(int SupplierId);
}
