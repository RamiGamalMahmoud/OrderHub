namespace OrderHub.UI.Messaging;

public static class CitiesMessages
{
    // The existing create command returns success only; listeners reload their lists.
    public sealed record CityCreatedMessage;

    public sealed record CityUpdatedMessage(int CityId);

    public sealed record CityDeletedMessage(int CityId);
}
