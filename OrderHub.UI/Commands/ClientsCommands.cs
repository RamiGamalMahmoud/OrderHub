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

public partial class ClientsCommands : AuthorizableCommandGroup, ICommandGroup
{
    private readonly IMediator _mediator;

    public ClientsCommands(IAuthorizationService authorization, IMediator mediator)
        : base(authorization)
    {
        _mediator = mediator;
        RegisterAuthorizableCommands(OpenCommand, CreateCommand, EditCommand, DeleteCommand);
    }

    private bool CanRead => Authorization.CanRead(PermissionFeature.Clients);

    private bool CanWrite => Authorization.CanWrite(PermissionFeature.Clients);

    [RelayCommand(CanExecute = nameof(CanRead))]
    private void Open()
    {
        NavigationService.Instance
            .NavigateTo<Features.Clients.Index.View>();
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Create()
    {
        await DialogService.Instance.ShowDialog<Features.Clients.Create.View>("إضافة عميل جديد");
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Edit(int id)
    {
        await DialogService.Instance.ShowDialog<Features.Clients.Update.View>("تعديل بيانات عميل", id);
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Delete(int id)
    {
        var item = await _mediator.Send(new Application.Queries.ClientQueries.GetClientByIdQuery(id));
        if (item is null)
            return;

        if (!DialogService.Instance.Confirm($"هل تريد حذف العميل ( {item.Name} )؟"))
            return;
        Result result = await _mediator.Send(new Application.Commands.ClienCommands.DeleteClientCommand(id));
        if (result.IsSuccess)
        {
            NotificationService.Instance.ShowSuccess("تم حذف العميل.");
            WeakReferenceMessenger.Default.Send(new ClientsMessages.ClientDeletedMessage(id));
        }

        else
        {
            NotificationService.Instance.ShowError(result.ErrorMessage);
        }
    }
}
