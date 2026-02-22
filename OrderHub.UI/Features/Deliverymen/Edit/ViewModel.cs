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
    private readonly IMessenger _messenger;
    private readonly ISelectionStore<IDeliverymanMarker, int> _selectionStore;

    public ViewModel(IMediator mediator, IMessenger messenger, ISelectionStore<IDeliverymanMarker, int> selectionStore, IDialogService dialogService) : base(mediator, dialogService)
    {
        _messenger = messenger;
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
        DeliverymanEditDto deliverymanEditDto = await _mediator.Send(new Application.Queries.DeliverymanQueries.GetDeliverymanForEditQuery(_selectionStore.Id));

        Name = deliverymanEditDto.Name;
        SelectedCity = Cities.Where(c => c.Id == deliverymanEditDto.Id).FirstOrDefault();
    }

    protected override async Task Save()
    {
        DeliverymanUpdateDto updateDto = new DeliverymanUpdateDto(_selectionStore.Id, Name, SelectedCity.Id);
        Result result = await _mediator.Send(new Application.Commands.DeliverymanCommands.UpdateDeliverymanCommand(updateDto));
        if(result.IsSuccess)
        {
            await _mediator.Publish(new Application.Notifications.SuccessNotification("تم تعديل بيانات المندوب بنجاح."));
            _messenger.Send(new Application.Messages.Deliveryman.DeleverymanUpdateMessage());
            OnRequestClose();
        }
        else
        {
            await _mediator.Publish(new Application.Notifications.ErrorNotification("خطأ : لم يتم تعديل بيانات المندوب."));
        }
    }
}
