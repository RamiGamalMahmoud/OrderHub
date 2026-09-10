using OrderHub.Application.Interfaces.Services;

namespace OrderHub.Application.Context;
public interface IApplicationContext
{
    ApplicationInfo Application { get; }

    ConnectionState Connection { get; }

    UserSession User { get; }

    UpdateInfo Update { get; }

    void Initialize(ApplicationInfo applicationInfo);
    void SetConnectionState(ConnectionState connectionState);
    void SetUser(UserSession user);
    void SetUpdate(UpdateInfo updateInfo);
    void ClearUser();
}