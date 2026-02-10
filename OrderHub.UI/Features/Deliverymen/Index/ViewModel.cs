using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using OrderHub.UI.Stores.Markers;
using System.Collections.Generic;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.DeliverymanDtos;

namespace OrderHub.UI.Features.Deliverymen.Index;

public partial class ViewModel : IndexViewModelBase<DeliverymanListDto>
{
    private readonly ISelectionStore<IDeliverymanMarker, int> _selectionStore;
    private readonly IDialogService _dialogService;
    private IEnumerable<DeliverymanListDto> _deliverymen;

    public ViewModel(IMessenger messenger, IMediator mediator, ISelectionStore<IDeliverymanMarker, int> selectionStore, IDialogService dialogService) : base(mediator,messenger)
    {
        _selectionStore = selectionStore;
        _dialogService = dialogService;

        _messenger.Register<Application.Messages.Deliveryman.DeliverymanCreatedMessage>(this, async (r, m) => await ReloadAsync());
        _messenger.Register<Application.Messages.Deliveryman.DeleverymanUpdateMessage>(this, async (r, m) => await ReloadAsync());
        _messenger.Register<Application.Messages.Deliveryman.DeliverymanDeletedMessage>(this, async (r, m) => await ReloadAsync());
    }

    protected override async Task LoadAsync()
    {
        Deliverymen = await _mediator.Send(new Application.Queries.DeliverymanQueries.GetAllDeliverymenListQuery());
    }

    protected override async Task ReloadAsync()
    {
        Deliverymen = await _mediator.Send(new Application.Queries.DeliverymanQueries.GetAllDeliverymenListQuery());
    }

    protected override async Task DeleteAsync(DeliverymanListDto deliveryman)
    {
        if (_dialogService.Confirm("") is not true)
        {
            return;
        }
        Result result = await _mediator.Send(new Application.Commands.DeliverymanCommands.DeleteDeliverymanCommand(deliveryman.Id));

        if (result.IsSuccess)
        {
            await _mediator.Publish(new Application.Notifications.SuccessNotification("تم حذف المندوب بنجاح."));
            _messenger.Send(new Application.Messages.Deliveryman.DeliverymanDeletedMessage());
        }
        else
        {
            await _mediator.Publish(new Application.Notifications.ErrorNotification("حدث خطأ أثناء حذف المندوب."));
        }
    }

    protected override Task ShowEditAsync(DeliverymanListDto deliveryman)
    {
        _selectionStore.Id = deliveryman.Id;
        _dialogService.ShowDialog<Edit.View>();
        return Task.CompletedTask;
    }

    protected override Task ShowCreateAsync()
    {
        _dialogService.ShowDialog<Create.View>();
        return Task.CompletedTask;
    }

    public IEnumerable<DeliverymanListDto> Deliverymen
    {
        get => _deliverymen;
        set => SetProperty(ref _deliverymen, value);
    }
}
