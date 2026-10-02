using OrderHub.Application.Context;

namespace OrderHub.Web.Services;

// Trusted server-side snapshot for one executor-created scope, including anonymous operations.
public sealed class OperationUser : ICurrentUser
{
    public bool IsInitialized { get; private set; }
    private int? _userId;

    public void Initialize(int? userId)
    {
        if (IsInitialized)
            throw new InvalidOperationException("Operation identity has already been initialized.");
        _userId = userId;
        IsInitialized = true;
    }

    public Task<int?> GetUserIdAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_userId);
}
