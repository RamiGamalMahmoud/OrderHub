using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;

namespace OrderHub.UI.Features.MainWindow;

internal record MainWindowCommand(
    string Name,
    string Icon = null,
    Action Action = null,
    bool IsEnabled = true,
    List<MainWindowCommand> ChildrenCommands = null)
{
    public RelayCommand RelayCommand { get; } = new RelayCommand(Action);
}
