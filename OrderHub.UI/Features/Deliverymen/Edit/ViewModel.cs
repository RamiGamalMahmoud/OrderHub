using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Interfaces;
using OrderHub.UI.Stores.Markers;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.DeliverymanDtos;

namespace OrderHub.UI.Features.Deliverymen.Edit;

internal class ViewModel : Editor.ViewModel
{
    private readonly ISelectionStore<IDeliverymanMarker, int> _selectionStore;

    public ViewModel(IMediator mediator, ISelectionStore<IDeliverymanMarker, int> selectionStore) : base(mediator)
    {
        _selectionStore = selectionStore;
    }

    public override string Title => "تعديل بيانات مندوب";

    public override void Dispose()
    {
        _selectionStore.Clear();
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        DeliverymanFormDto deliveryman = await _mediator.Send(new Application.Queries.DeliverymanQueries.GetDeliverymanForEditQuery(_selectionStore.Id));

        Name = deliveryman.Name;
        SelectedCity = Cities.Where(c => c.Id == deliveryman.CityId).FirstOrDefault();
        PhoneNumber = deliveryman.PhoneNumber;
        SelectedWhatsappGroup = WhatsappGroups.Where(group => group.Id == deliveryman.WhatsappGroupId).FirstOrDefault();
    }

    protected override async Task Save()
    {
        int? whatsappGroupId = SelectedWhatsappGroup?.Id > 0 ? SelectedWhatsappGroup.Id : null;
        DeliverymanFormDto deliveryman = new DeliverymanFormDto(Name, SelectedCity.Id, PhoneNumber, whatsappGroupId);
        Result result = await _mediator.Send(new Application.Commands.DeliverymanCommands.UpdateDeliverymanCommand(_selectionStore.Id, deliveryman));
        if(result.IsSuccess)
        {
            await _mediator.Publish(new Application.Notifications.SuccessNotification("تم تعديل بيانات المندوب بنجاح."));
            WeakReferenceMessenger.Default.Send(new Application.Messages.Deliveryman.DeleverymanUpdateMessage());
            OnRequestClose();
        }
        else
        {
            await _mediator.Publish(new Application.Notifications.ErrorNotification("خطأ : لم يتم تعديل بيانات المندوب."));
        }
    }
}
