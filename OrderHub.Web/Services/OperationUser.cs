using OrderHub.Application.Context;

namespace OrderHub.Web.Services;

// Trusted server-side snapshot for one executor-created scope, including anonymous operations.
public sealed class OperationUser : ICurrentUser
{
    private CurrentUserIdentity? _identity;
    private CurrentUserIdentity Identity => _identity
        ?? throw new InvalidOperationException("Operation identity has not been initialized.");
    public bool IsInitialized => _identity is not null;
    public int? Id => Identity.Id;
    public string? UserName => Identity.UserName;
    public string? DisplayName => Identity.DisplayName;
    public bool IsAuthenticated => Identity.IsAuthenticated;
    public bool IsAdmin => Identity.IsAdmin;

    public void Initialize(CurrentUserIdentity identity)
    {
        if (IsInitialized)
            throw new InvalidOperationException("Operation identity has already been initialized.");
        ArgumentNullException.ThrowIfNull(identity);
        _identity = identity;
    }

}
