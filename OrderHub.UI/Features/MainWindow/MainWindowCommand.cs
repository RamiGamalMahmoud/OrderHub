using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace OrderHub.UI.Features.MainWindow;

internal record MainWindowCommand(
    string Name,
    string Icon = null,
    Action Action = null,
    bool IsEnabled = true,
    List<MainWindowCommand> ChildrenCommands = null)
{
    public ICommand Command { get; } = new RelayCommand(Action);
}
