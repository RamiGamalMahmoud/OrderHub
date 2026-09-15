using OrderHub.Application.Interfaces.Services;
using System;

namespace OrderHub.Application.Context;
public sealed class ApplicationContext : IApplicationContext
{
    public ApplicationInfo Application { get; private set; }

    public ConnectionState Connection { get; private set; }

    public UserSession User { get; private set; }

    public void SetConnectionState(ConnectionState state)
    {
        Connection = state;
    }

    public void SetUser(UserSession user)
    {
        User = user;
        OnUserChanged();
    }

    public void ClearUser()
    {
        User = null;
        OnUserChanged();
    }

    public void Initialize(ApplicationInfo applicationInfo)
    {
        Application ??= applicationInfo;
    }

    private UpdateInfo _update;

    public event EventHandler UserChanged;

    public UpdateInfo Update => _update;

    public void SetUpdate(UpdateInfo updateInfo)
    {
        _update = updateInfo;
    }

    private void OnUserChanged() => UserChanged?.Invoke(this, EventArgs.Empty);
}