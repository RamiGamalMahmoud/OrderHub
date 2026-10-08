namespace OrderHub.UI.States;
public class AppState : IAppState
{
    public AppState(
        SallaState salla,
        ConnectionState connection,
        MessagingState messaging,
        UpdateState update)
    {
        Salla = salla;
        Connection = connection;
        Messageing = messaging;
        Update = update;
    }

    public SallaState Salla { get; }
    public ConnectionState Connection { get; }
    public MessagingState Messageing { get; }
    public UpdateState Update { get; }

    public static IAppState Instance { get; private set; } = null;

    public static void SetInstance(IAppState appState) => Instance ??= appState;
}
