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

public partial class DeliverymenCommands : AuthorizableCommandGroup, ICommandGroup
{
    private readonly IMediator _mediator;

    public DeliverymenCommands(IAuthorizationService authorization, IMediator mediator)
        : base(authorization)
    {
        _mediator = mediator;
        RegisterAuthorizableCommands(OpenCommand, CreateCommand, EditCommand, DeleteCommand);
    }

    private bool CanRead => Authorization.CanRead(PermissionFeature.Deliverymen);

    private bool CanWrite => Authorization.CanWrite(PermissionFeature.Deliverymen);

    [RelayCommand(CanExecute = nameof(CanRead))]
    private void Open()
    {
        NavigationService.Instance
            .NavigateTo<Features.Deliverymen.Index.View>();
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Create()
    {
        await DialogService.Instance.ShowDialog<Features.Deliverymen.Create.View>("إضافة مندوب جديد");
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Edit(int id)
    {
        await DialogService.Instance.ShowDialog<Features.Deliverymen.Edit.View>("تعديل بيانات مندوب", id);
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Delete(int id)
    {
        var item = await _mediator.Send(new Application.Queries.DeliverymanQueries.GetDeliverymanByIdQuery(id));
        if (item is null)
            return;

        if (DialogService.Instance.Confirm($"هل تريد حذف المندوب ({item.Name})؟") is not true)
        {
            return;
        }
        Result result = await _mediator.Send(new Application.Commands.DeliverymanCommands.DeleteDeliverymanCommand(id));

        if (result.IsSuccess)
        {
            NotificationService.Instance.ShowSuccess("تم حذف المندوب بنجاح.");
            WeakReferenceMessenger.Default.Send(new DeliverymenMessages.DeliverymanDeletedMessage(id));
        }
        else
        {
            NotificationService.Instance.ShowError("حدث خطأ أثناء حذف المندوب.");
        }
    }
}
