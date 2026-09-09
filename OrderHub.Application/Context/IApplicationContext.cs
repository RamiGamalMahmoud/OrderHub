namespace OrderHub.Application.Context;
public interface IApplicationContext
{
    ApplicationInfo Application { get; }

    ConnectionState Connection { get; }

    UserSession User { get; }

    void Initialize(ApplicationInfo applicationInfo);
    void SetConnectionState(ConnectionState connectionState);
    void SetUser(UserSession user);
    public void ClearUser();
}