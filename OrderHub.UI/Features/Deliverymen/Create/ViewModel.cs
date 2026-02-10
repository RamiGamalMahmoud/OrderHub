using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.DeliverymanDtos;

namespace OrderHub.UI.Features.Deliverymen.Create;

internal class ViewModel : Editor.ViewModel
{
    private readonly IMessenger _messenger;

    public ViewModel(IMediator mediator, IMessenger messenger) : base(mediator)
    {
        _messenger = messenger;
    }

    public override string Title => "إنشاء مندوب جديد";

    protected override async Task Save()
    {
        DeliverymanCreateDto deliverymanCreateDto = new DeliverymanCreateDto(Name, SelectedCity.Id);
        Result result = await _mediator.Send(new Application.Commands.DeliverymanCommands.CreateDeliverymanCommand(deliverymanCreateDto));
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
