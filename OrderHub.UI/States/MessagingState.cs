using CommunityToolkit.Mvvm.ComponentModel;

namespace OrderHub.UI.States;

public sealed class MessagingState : ObservableObject, IState
{
    private bool _isConnected;
    private bool _isMessageServiceRunning;

    public bool IsConnected
    {
        get => _isConnected;
        private set => SetProperty(ref _isConnected, value);
    }

    public bool IsMessageServiceRunning
    {
        get => _isMessageServiceRunning;
        private set => SetProperty(ref _isMessageServiceRunning, value);
    }

    internal void SetConnected(bool value)
    {
        IsConnected = value;
    }

    internal void SetMessageServiceRunning(bool value)
    {
        IsMessageServiceRunning = value;
    }
}