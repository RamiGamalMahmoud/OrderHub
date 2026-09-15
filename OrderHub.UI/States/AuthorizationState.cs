using CommunityToolkit.Mvvm.ComponentModel;
using OrderHub.Application.Context;

namespace OrderHub.UI.States;

public sealed class AuthorizationState : ObservableObject, IState
{
    private bool _isAdmin;

    public bool IsAdmin
    {
        get => _isAdmin;
        private set => SetProperty(ref _isAdmin, value);
    }

    internal void SetUser(UserSession user)
    {
        IsAdmin = user?.IsAdmin == true;
    }

    internal void Clear()
    {
        IsAdmin = false;
    }
}