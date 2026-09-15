using CommunityToolkit.Mvvm.Input;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Enums;
using OrderHub.UI.Services;

namespace OrderHub.UI.Commands;

public partial class SuppliersCommands : AuthorizableCommandGroup, ICommandGroup
{
    public SuppliersCommands(IAuthorizationService authorization)
        : base(authorization)
    {
        RegisterAuthorizableCommands(OpenCommand);
    }

    private bool CanRead => Authorization.CanRead(PermissionFeature.Suppliers);

    [RelayCommand(CanExecute = nameof(CanRead))]
    private void Open()
    {
        NavigationService.Instance
            .NavigateTo<Features.Suppliers.Index.View>();
    }
}
