using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using OrderHub.UI.Stores.Markers;
using System.Collections.Generic;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ShippingCarriersDtos;

namespace OrderHub.UI.Features.ShippingCarriers.Index;

public partial class ViewModel : IndexViewModelBase<ShippingCarrierListDto>
{
    private readonly ISelectionStore<IShippingCarrierMarker, int> _selectionStore;
    private readonly IDialogService _dialogService;
    private IEnumerable<ShippingCarrierListDto> _shippingCarriers;

    public ViewModel(IMediator mediator, IMessenger messenger, ISelectionStore<IShippingCarrierMarker, int> selectionStore, IDialogService dialogService) : base(mediator, messenger)
    {
        _selectionStore = selectionStore;
        _dialogService = dialogService;
    }

    protected override async Task DeleteAsync(ShippingCarrierListDto model)
    {
        if(!_dialogService.Confirm("هل تريد حذف شركة الشحن؟")) return;
        Result result = await _mediator.Send(new Application.Commands.ShippingCarriersCommands.DeleteShippingCarrierCommand(model.Id));

        if(result.IsSuccess)
        {
            string message = MessageBuilder.Build(MessageBuilder.OperationType.Delete, true, "شركة شحن");
            await _mediator.Publish(new Application.Notifications.SuccessNotification(message));
            _messenger.Send(new Application.Messages.ShippingCarriers.ShippingCarrierDeletedMessage());
            await ReloadAsync();
        }

        else
        {
            string message = MessageBuilder.Build(MessageBuilder.OperationType.Delete, false, "شركة شحن");
            await _mediator.Publish(new Application.Notifications.ErrorNotification(message));
        }
    }

    protected override async Task LoadAsync()
    {
        ShippingCarriers = await _mediator.Send(new Application.Queries.ShippingCarriersQueries.GetShippingCarriersQuery());
    }

    protected override async Task ReloadAsync()
    {
        ShippingCarriers = await _mediator.Send(new Application.Queries.ShippingCarriersQueries.GetShippingCarriersQuery());
    }

    protected override Task ShowCreateAsync()
    {
        _dialogService.ShowDialog<Create.View>();
        return Task.CompletedTask;
    }

    protected override Task ShowEditAsync(ShippingCarrierListDto model)
    {
        _selectionStore.Id = model.Id;
        _dialogService.ShowDialog<Edit.View>();
        return Task.CompletedTask;
    }

    public IEnumerable<ShippingCarrierListDto> ShippingCarriers { get => _shippingCarriers; private set => SetProperty(ref _shippingCarriers, value); }
}
