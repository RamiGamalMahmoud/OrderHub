namespace OrderHub.UI.Messaging;

public static class UsersMessages
{
    public record UserActivatedMessage(int UserId);

    public record UserDeactivatedMessage(int UserId);

    // The create command returns success only; listeners reload through a query.
    public sealed record UserCreatedMessage;

    public sealed record UserUpdatedMessage(int UserId);
}
