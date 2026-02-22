using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ShippingCarriersDtos;

namespace OrderHub.UI.Features.ShippingCarriers.Create;

public class ViewModel : Editor.ViewModel
{
    private readonly IMessenger _messenger;

    public ViewModel(IMediator mediator, IMessenger messenger, IDialogService dialogService) : base(mediator, dialogService)
    {
        _messenger = messenger;
    }

    public override string Title => "إنشاء شركة شحن جديدة";

    protected override async Task Save()
    {
        decimal.TryParse(ShippingCostText, out decimal shippingCost);
        ShippingCarrierCreateDto dto = new ShippingCarrierCreateDto(Name, shippingCost, CountryCode, PhoneNumber, SelectedCity.Id, Street);
        Result result = await _mediator.Send(new Application.Commands.ShippingCarriersCommands.CreateShippingCarrierCommand(dto));
        if (result.IsSuccess)
        {
            string message = MessageBuilder.Build(MessageBuilder.OperationType.Create, true, "شركة شحن");
            await _mediator.Publish(new Application.Notifications.SuccessNotification(message));
            _messenger.Send(new Application.Messages.ShippingCarriers.ShippingCarrierCreatedMessage());
            OnRequestClose();
        }
        else
        {
            string message = MessageBuilder.Build(MessageBuilder.OperationType.Create, false, "شركة شحن");
            await _mediator.Publish(new Application.Notifications.ErrorNotification(message));
        }
    }
}
