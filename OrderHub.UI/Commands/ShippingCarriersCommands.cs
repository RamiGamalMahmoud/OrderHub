using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.UI.Common;
using OrderHub.UI.Messaging;
using OrderHub.UI.Services;
using System.Threading.Tasks;

namespace OrderHub.UI.Commands;

public partial class ShippingCarriersCommands : AuthorizableCommandGroup, ICommandGroup
{
    private readonly IMediator _mediator;

    public ShippingCarriersCommands(IAuthorizationService authorization, IMediator mediator)
        : base(authorization)
    {
        _mediator = mediator;
        RegisterAuthorizableCommands(OpenCommand, CreateCommand, EditCommand, DeleteCommand);
    }

    private bool CanRead => Authorization.CanRead(PermissionFeature.ShippingCarriers);

    private bool CanWrite => Authorization.CanWrite(PermissionFeature.ShippingCarriers);

    [RelayCommand(CanExecute = nameof(CanRead))]
    private void Open()
    {
        NavigationService.Instance
            .NavigateTo<Features.ShippingCarriers.Index.View>();
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Create()
    {
        await DialogService.Instance.ShowDialog<Features.ShippingCarriers.Create.View>("إضافة شركة شحن جديدة");
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Edit(int id)
    {
        await DialogService.Instance.ShowDialog<Features.ShippingCarriers.Edit.View>("تعديل بيانات شركة شحن", id);
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Delete(int id)
    {
        if(!DialogService.Instance.Confirm("هل تريد حذف شركة الشحن؟")) return;
        Result result = await _mediator.Send(new Application.Commands.ShippingCarriersCommands.DeleteShippingCarrierCommand(id));

        if(result.IsSuccess)
        {
            string message = MessageBuilder.Build(MessageBuilder.OperationType.Delete, true, "شركة شحن");
            NotificationService.Instance.ShowSuccess(message);
            WeakReferenceMessenger.Default.Send(new ShippingCarriersMessages.ShippingCarrierDeletedMessage(id));
        }

        else
        {
            string message = MessageBuilder.Build(MessageBuilder.OperationType.Delete, false, "شركة شحن");
            NotificationService.Instance.ShowError(message);
        }
    }
}
