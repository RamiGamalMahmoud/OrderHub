using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.UI.Messaging;
using OrderHub.UI.Services;
using System.Threading.Tasks;

namespace OrderHub.UI.Commands;

public partial class CitiesCommands : AuthorizableCommandGroup, ICommandGroup
{
    private readonly IMediator _mediator;

    public CitiesCommands(IAuthorizationService authorization, IMediator mediator)
        : base(authorization)
    {
        _mediator = mediator;
        RegisterAuthorizableCommands(OpenCommand, CreateCommand, EditCommand, DeleteCommand);
    }

    private bool CanRead => Authorization.CanRead(PermissionFeature.Cities);

    private bool CanWrite => Authorization.CanWrite(PermissionFeature.Cities);

    [RelayCommand(CanExecute = nameof(CanRead))]
    private void Open()
    {
        NavigationService.Instance
            .NavigateTo<Features.Cities.Index.View>();
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Create()
    {
        await DialogService.Instance.ShowDialog<Features.Cities.Create.CreateCityView>("إضافة مدينة جديدة");
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Edit(int id)
    {
        await DialogService.Instance.ShowDialog<Features.Cities.Edit.EditCityView>("إضافة مدينة جديدة", id);
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Delete(int id)
    {
        var item = await _mediator.Send(new Application.Queries.CityQueries.GetCityForEditQuery(id));
        if (item is null)
            return;

        if (!DialogService.Instance.Confirm($"هل تريد حذف المدينة ({item.Name})؟"))
        {
            return;
        }

        Result result = await _mediator.Send(new Application.Commands.CityCommands.DeleteCityCommand(id));
        if (result.IsSuccess)
        {
            NotificationService.Instance.ShowSuccess($"تم حذف المدينة ({item.Name}) بنجاح.");
            WeakReferenceMessenger.Default.Send(new CitiesMessages.CityDeletedMessage(id));
            return;
        }

        NotificationService.Instance.ShowError(result.ErrorMessage);
    }
}
