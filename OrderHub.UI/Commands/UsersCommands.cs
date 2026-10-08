using CommunityToolkit.Mvvm.Input;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Enums;
using OrderHub.UI.Services;
using System.Threading.Tasks;

namespace OrderHub.UI.Commands;

public partial class UsersCommands : AuthorizableCommandGroup, ICommandGroup
{
    public UsersCommands(IAuthorizationService authorization)
        : base(authorization)
    {
        RegisterAuthorizableCommands(OpenCommand, CreateCommand, EditCommand);
    }

    private bool CanRead => Authorization.CanRead(PermissionFeature.Users);

    private bool CanWrite => Authorization.CanWrite(PermissionFeature.Users);

    [RelayCommand(CanExecute = nameof(CanRead))]
    private void Open()
    {
        NavigationService.Instance
            .NavigateTo<Features.Administration.Users.List.UsersView>();
    }
    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Create()
    {
        await DialogService.Instance.ShowDialog<Features.Administration.Users.Create.CreateUserView>("إضافة مستخدم");
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Edit(int id)
    {
        await DialogService.Instance.ShowDialog<Features.Administration.Users.Details.UserDetailsView>("", id);
    }
}
