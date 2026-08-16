using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using OrderHub.UI.Stores.Markers;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ShippingCarriersDtos;

namespace OrderHub.UI.Features.ShippingCarriers.Edit;

public class ViewModel : Editor.ViewModel
{
    private readonly ISelectionStore<IShippingCarrierMarker, int> _selectionStore;

    public ViewModel(IMediator mediator, ISelectionStore<IShippingCarrierMarker, int> selectionStore) : base(mediator)
    {
        _mediator = mediator;
        _selectionStore = selectionStore;
    }

    public override async Task LoadAsync()
    {
        await base.LoadAsync();
        ShippingCarrierEditDto dto = await _mediator.Send(new Application.Queries.ShippingCarriersQueries.GetShippingCarrierForEditQuery(_selectionStore.Id));
        if (dto is null)
        {
            await _mediator.Publish(new Application.Notifications.ErrorNotification("خطأ! لم يتم تحميل بيانات شركة الشحن للتعديل."));
            await Task.Delay(3000);
            OnRequestClose();
            return;
        }

        Name = dto.Name;
        ShippingCostText = dto.ShippingCost.ToString();

        SelectedCity = Cities.Where(c => c.Id == dto.CityId).SingleOrDefault();
        Street = dto.Street;

        CountryCode = dto.CountryCode;
        PhoneNumber = dto.PhoneNumber;

        HasChanges = false;

    }

    public override string Title => "تعديل بيانات شركة شحن";

    protected override async Task Save()
    {
        decimal.TryParse(ShippingCostText, out decimal shippingCost);
        ShippingCarrierUpdateDto dto = new ShippingCarrierUpdateDto(_selectionStore.Id, Name, shippingCost, CountryCode, PhoneNumber, SelectedCity.Id, Street);
        Result result = await _mediator.Send(new Application.Commands.ShippingCarriersCommands.UpdateShippingCarrierCommand(dto));
        if (result.IsSuccess)
        {
            string message = MessageBuilder.Build(MessageBuilder.OperationType.Update, true, "شركة شحن");
            await _mediator.Publish(new Application.Notifications.SuccessNotification(message));
            WeakReferenceMessenger.Default.Send(new Application.Messages.ShippingCarriers.ShippingCarrierUpdatedMessage());
            OnRequestClose();
        }
        else
        {
            string message = MessageBuilder.Build(MessageBuilder.OperationType.Update, false, "شركة شحن");
            await _mediator.Publish(new Application.Notifications.ErrorNotification(message));
        }
    }
}
