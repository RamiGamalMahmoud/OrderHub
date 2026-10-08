namespace OrderHub.Web.Services;

// Immutable values captured at the Web authentication boundary for one operation.
public sealed record CurrentUserIdentity(
    int? Id,
    string? UserName,
    string? DisplayName,
    bool IsAuthenticated,
    bool IsAdmin);
