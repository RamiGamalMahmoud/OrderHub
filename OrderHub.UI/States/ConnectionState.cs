using CommunityToolkit.Mvvm.ComponentModel;
using OrderHub.Application.Interfaces.Services;
using OrderHub.UI.States;

public partial class ConnectionState : ObservableObject, IState
{
    public ConnectionState(IConnectionService connectionService)
    {
        IsConnected = connectionService.IsConnected;
        connectionService.ConnectionChanged += OnConnectionChanged;
    }

    private bool _isConnected;

    public bool IsConnected
    {
        get => _isConnected;
        private set => SetProperty(ref _isConnected, value);
    }

    private void OnConnectionChanged(
        object sender,
        bool isConnected)
    {
        IsConnected = isConnected;
    }
}