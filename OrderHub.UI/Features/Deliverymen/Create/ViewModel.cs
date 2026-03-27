using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Interfaces;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.DeliverymanDtos;

namespace OrderHub.UI.Features.Deliverymen.Create;

internal class ViewModel : Editor.ViewModel
{
    private readonly IMessenger _messenger;

    public ViewModel(IMediator mediator, IMessenger messenger, IDialogService dialogService) : base(mediator, dialogService)
    {
        _messenger = messenger;
    }

    public override string Title => "إنشاء مندوب جديد";

    protected override async Task Save()
    {
        DeliverymanFormDto deliveryman = new DeliverymanFormDto(Name, SelectedCity.Id, PhoneNumber);
        Result result = await _mediator.Send(new Application.Commands.DeliverymanCommands.CreateDeliverymanCommand(deliveryman));
        if (result.IsSuccess)
        {
            await _mediator.Publish(new Application.Notifications.SuccessNotification("تم انشاء المندوب بنجاح"));
            _messenger.Send(new Application.Messages.Deliveryman.DeliverymanCreatedMessage());
            OnRequestClose();
        }
        else
        {
            await _mediator.Publish(new Application.Notifications.ErrorNotification("خطأ أثناء إنشاء مندوب"));
        }
    }
}
