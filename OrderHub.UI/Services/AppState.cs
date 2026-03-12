using CommunityToolkit.Mvvm.ComponentModel;
using OrderHub.Application.Interfaces.Services;

namespace OrderHub.UI.Services;

public partial class AppState : ObservableObject, IAppState
{
    private readonly IConnectionService _connectionService;

    public AppState(IConnectionService connectionService)
    {
        _connectionService = connectionService;
        _connectionService.ConnectionChanged += ConnectionServiceOnConnectionChanged;
        _connectionService.Start();
    }

    private void ConnectionServiceOnConnectionChanged(object sender, bool e)
    {
        IsConnected = e;
    }

    [ObservableProperty]
    private bool _isConnected;
}
