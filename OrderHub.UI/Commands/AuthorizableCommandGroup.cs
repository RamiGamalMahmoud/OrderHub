using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrderHub.Application.Interfaces.Services;
using System;
using System.Collections.Generic;

namespace OrderHub.UI.Commands;

public abstract class AuthorizableCommandGroup : ObservableObject
{
    private readonly List<IRelayCommand> _authorizableCommands = [];

    protected IAuthorizationService Authorization { get; }

    protected AuthorizableCommandGroup(
        IAuthorizationService authorization)
    {
        Authorization = authorization;

        Authorization.AuthorizationChanged += OnAuthorizationChanged;
    }

    protected void RegisterAuthorizableCommands(
        params IRelayCommand[] commands)
    {
        _authorizableCommands.AddRange(commands);
    }

    private void OnAuthorizationChanged(
        object sender,
        EventArgs e)
    {
        foreach (IRelayCommand command in _authorizableCommands)
        {
            command.NotifyCanExecuteChanged();
        }
    }
}