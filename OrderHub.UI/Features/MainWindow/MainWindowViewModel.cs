using CommunityToolkit.Mvvm.ComponentModel;
using OrderHub.Application.Context;

namespace OrderHub.UI.Features.MainWindow;

internal partial class MainWindowViewModel : ObservableObject
{
    public MainWindowViewModel(
        IApplicationContext applicationContext)
    {
        ApplicationContext = applicationContext;

        Commands = new MainWindowCommands(applicationContext);
    }

    public MainWindowCommands Commands { get; }

    public IApplicationContext ApplicationContext { get; }

    public string Title =>
        $"OrderHub - {ApplicationContext.Application.InstallationType} - v{ApplicationContext.Application.Version}";

    [ObservableProperty]
    private string _message;

    public bool IsAppInDebug =>
#if DEBUG
        true;
#else
        false;
#endif
}