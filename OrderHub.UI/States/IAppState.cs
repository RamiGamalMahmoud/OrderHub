namespace OrderHub.UI.States;
public interface IAppState
{
    SallaState Salla { get; }
    ConnectionState Connection { get; }
    MessagingState Messageing { get; }
    UpdateState Update { get; }
}
