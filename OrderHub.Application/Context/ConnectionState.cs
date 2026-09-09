namespace OrderHub.Application.Context;
public sealed record ConnectionState(
    bool IsInternetAvailable,
    bool IsDatabaseAvailable);