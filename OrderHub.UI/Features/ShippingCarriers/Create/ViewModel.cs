using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Services;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ShippingCarriersDtos;

namespace OrderHub.UI.Features.ShippingCarriers.Create;

public class ViewModel : Editor.ViewModel
{
    public ViewModel(IMediator mediator) : base(mediator)
    {
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
            NotificationService.Instance.ShowSuccess(message);
            WeakReferenceMessenger.Default.Send(new Application.Messages.ShippingCarriers.ShippingCarrierCreatedMessage());
            OnRequestClose();
        }
        else
        {
            string message = MessageBuilder.Build(MessageBuilder.OperationType.Create, false, "شركة شحن");
            NotificationService.Instance.ShowError(message);
        }
    }
}
