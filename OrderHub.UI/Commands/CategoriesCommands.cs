using CommunityToolkit.Mvvm.Input;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Enums;
using OrderHub.UI.Services;
using System.Threading.Tasks;

namespace OrderHub.UI.Commands;

public partial class CategoriesCommands : AuthorizableCommandGroup, ICommandGroup
{
    private readonly IRequestExecutor _requestExecutor;
    public CategoriesCommands(
        IAuthorizationService authorization,
        IRequestExecutor requestExecutor) : base(authorization)
    {
        RegisterAuthorizableCommands(
            OpenCommand,
            CreateCommand,
            EditCommand,
            DeleteCommand);
        _requestExecutor = requestExecutor;
    }

    private bool CanRead => Authorization.CanRead(PermissionFeature.Categories);
    private bool CanWrite => Authorization.CanWrite(PermissionFeature.Categories);

    [RelayCommand(CanExecute = nameof(CanRead))]
    private void Open()
    {
        NavigationService.Instance
            .NavigateTo<Features.Categories.Index.View>();
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Create()
    {
        await DialogService.Instance.ShowDialog<Features.Categories.Create.View>("");
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Edit(int categoryId)
    {
        await DialogService.Instance.ShowDialog<Features.Categories.Update.View>("", categoryId);
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Delete(int categoryId)
    {
        await _requestExecutor.ExecuteAsync(new Application.Commands.CategoryCommands.DeleteCategoryCommand(categoryId));
    }
}
