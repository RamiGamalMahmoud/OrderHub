using CommunityToolkit.Mvvm.ComponentModel;

namespace OrderHub.UI.States;
public partial class SallaState : ObservableObject, IState
{
    [ObservableProperty]
    private bool _isAuthenticated;
}
