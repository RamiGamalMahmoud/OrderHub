using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.DeliverymanDtos;

namespace OrderHub.UI.Features.Deliverymen.Create;

internal class ViewModel : Editor.ViewModel
{

    public ViewModel(IMediator mediator) : base(mediator)
    {
    }

    public override string Title => "إنشاء مندوب جديد";

    protected override async Task Save()
    {
        int? whatsappGroupId = SelectedWhatsappGroup?.Id > 0 ? SelectedWhatsappGroup.Id : null;
        DeliverymanFormDto deliveryman = new DeliverymanFormDto(Name, SelectedCity.Id, PhoneNumber, whatsappGroupId);
        Result result = await _mediator.Send(new Application.Commands.DeliverymanCommands.CreateDeliverymanCommand(deliveryman));
        if (result.IsSuccess)
        {
            await _mediator.Publish(new Application.Notifications.SuccessNotification("تم انشاء المندوب بنجاح"));
            WeakReferenceMessenger.Default.Send(new Application.Messages.Deliveryman.DeliverymanCreatedMessage());
            OnRequestClose();
        }
        else
        {
            await _mediator.Publish(new Application.Notifications.ErrorNotification("خطأ أثناء إنشاء مندوب"));
        }
    }
}
