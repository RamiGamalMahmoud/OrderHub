#nullable enable

namespace OrderHub.Application.Context;

public interface ICurrentUser
{
    int? Id { get; }
    string? UserName { get; }
    string? DisplayName { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
}
