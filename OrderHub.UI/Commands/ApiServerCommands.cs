using CommunityToolkit.Mvvm.Input;
using OrderHub.Application.Interfaces.Services;
using System.Diagnostics;

namespace OrderHub.UI.Commands;
public partial class ApiServerCommands : AuthorizableCommandGroup, ICommandGroup
{
    private const string SERVICE_NAME = "OrderHubApi";
    public ApiServerCommands(IAuthorizationService authorization) : base(authorization)
    {
    }

    [RelayCommand]
    private void Run()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = $"start {SERVICE_NAME}",
            UseShellExecute = true,
            Verb = "runas"
        });
    }

    [RelayCommand]
    private void Stop()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = $"stop {SERVICE_NAME}",
            UseShellExecute = true,
            Verb = "runas"
        });
    }
}
