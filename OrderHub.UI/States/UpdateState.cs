using CommunityToolkit.Mvvm.ComponentModel;

namespace OrderHub.UI.States;

public sealed class UpdateState : ObservableObject, IState
{
    private bool _isUpdating;
    private double _progress;

    public bool IsUpdating
    {
        get => _isUpdating;
        internal set => SetProperty(ref _isUpdating, value);
    }

    public double Progress
    {
        get => _progress;
        internal set => SetProperty(ref _progress, value);
    }
}