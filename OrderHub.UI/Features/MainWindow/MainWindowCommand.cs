using System.Collections.Generic;
using System.Windows.Input;

namespace OrderHub.UI.Features.MainWindow;

internal record MainWindowCommand(
    string Name,
    string Icon = null,
    ICommand Command = null,
    bool IsEnabled = true,
    List<MainWindowCommand> ChildrenCommands = null);
