using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrderHub.Application.Interfaces.Services;
using System.Diagnostics;

namespace OrderHub.UI.Commands;
public partial class ApplicationCommands : ObservableObject, ICommandGroup
{
    private readonly IApplicationDirectoriesService _applicationDirectoriesService;

    public ApplicationCommands(
        IApplicationDirectoriesService applicationDirectoriesService)
    {
        _applicationDirectoriesService = applicationDirectoriesService;
    }

    [RelayCommand]
    private void OpenDataFolder()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = _applicationDirectoriesService.DataDirectory,
            UseShellExecute = true,
            Verb = "open"
        });
    }

    [RelayCommand]
    private void Exit()
    {
        System.Windows.Application.Current.Shutdown();
    }
}