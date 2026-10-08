using System.Collections.Generic;
using System.Windows.Input;

namespace OrderHub.UI.Features.MainWindow;

internal record MainWindowCommand(
    string Name,
    string Icon = null,
    ICommand Command = null,
    List<MainWindowCommand> ChildrenCommands = null)
{
    public bool IsEnabled => Command is null || Command.CanExecute(null);
}
