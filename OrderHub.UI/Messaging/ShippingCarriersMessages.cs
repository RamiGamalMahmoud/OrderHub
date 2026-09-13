namespace OrderHub.UI.Messaging;

public static class ShippingCarriersMessages
{
    // The existing create command returns success only; listeners reload their lists.
    public sealed record ShippingCarrierCreatedMessage;

    public sealed record ShippingCarrierUpdatedMessage(int ShippingCarrierId);

    public sealed record ShippingCarrierDeletedMessage(int ShippingCarrierId);
}
