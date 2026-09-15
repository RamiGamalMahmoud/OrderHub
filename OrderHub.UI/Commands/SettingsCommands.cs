using CommunityToolkit.Mvvm.Input;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Enums;
using OrderHub.UI.Services;

namespace OrderHub.UI.Commands;

public partial class SettingsCommands : AuthorizableCommandGroup, ICommandGroup
{
    public SettingsCommands(IAuthorizationService authorization)
        : base(authorization)
    {
        RegisterAuthorizableCommands(OpenCommand);
    }

    private bool CanRead => Authorization.CanRead(PermissionFeature.Settings);

    [RelayCommand(CanExecute = nameof(CanRead))]
    private void Open()
    {
        NavigationService.Instance
            .NavigateTo<Features.Settings.Home.SettingsHomeView>();
    }
}
