namespace OrderHub.Application.Context;
public sealed record UserSession(
    int Id,
    string UserName,
    string DisplayName,
    bool IsAdmin);
