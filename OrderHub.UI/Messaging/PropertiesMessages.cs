namespace OrderHub.UI.Messaging;

public static class PropertiesMessages
{
    public sealed record PropertyCreatedMessage(int PropertyId);

    public sealed record PropertyUpdatedMessage(int PropertyId);
}
