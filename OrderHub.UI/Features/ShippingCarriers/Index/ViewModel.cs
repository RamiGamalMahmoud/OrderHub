using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using OrderHub.UI.Stores.Markers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ShippingCarriersDtos;

namespace OrderHub.UI.Features.ShippingCarriers.Index;

public partial class ViewModel : IndexViewModelBase<ShippingCarrierListDto>
{
    private readonly ISelectionStore<IShippingCarrierMarker, int> _selectionStore;
    private IEnumerable<ShippingCarrierListDto> _shippingCarriers;
    private List<ShippingCarrierListDto> _allShippingCarriers = [];

    public ViewModel(IMediator mediator, ISelectionStore<IShippingCarrierMarker, int> selectionStore) : base(mediator)
    {
        _selectionStore = selectionStore;

        WeakReferenceMessenger.Default.Register<Application.Messages.ShippingCarriers.ShippingCarrierCreatedMessage>(this, async (_, _) => await ReloadAsync());
        WeakReferenceMessenger.Default.Register<Application.Messages.ShippingCarriers.ShippingCarrierUpdatedMessage>(this, async (_, _) => await ReloadAsync());
        WeakReferenceMessenger.Default.Register<Application.Messages.ShippingCarriers.ShippingCarrierDeletedMessage>(this, async (_, _) => await ReloadAsync());
    }

    protected override async Task DeleteAsync(ShippingCarrierListDto model)
    {
        if(!DialogService.Instance.Confirm("هل تريد حذف شركة الشحن؟")) return;
        Result result = await _mediator.Send(new Application.Commands.ShippingCarriersCommands.DeleteShippingCarrierCommand(model.Id));

        if(result.IsSuccess)
        {
            string message = MessageBuilder.Build(MessageBuilder.OperationType.Delete, true, "شركة شحن");
            await _mediator.Publish(new Application.Notifications.SuccessNotification(message));
            WeakReferenceMessenger.Default.Send(new Application.Messages.ShippingCarriers.ShippingCarrierDeletedMessage());
        }

        else
        {
            string message = MessageBuilder.Build(MessageBuilder.OperationType.Delete, false, "شركة شحن");
            await _mediator.Publish(new Application.Notifications.ErrorNotification(message));
        }
    }

    protected override async Task LoadAsync()
    {
        _allShippingCarriers = (await _mediator.Send(new Application.Queries.ShippingCarriersQueries.GetShippingCarriersQuery())).ToList();
        ApplyFilter();
    }

    protected override async Task ReloadAsync()
    {
        _allShippingCarriers = (await _mediator.Send(new Application.Queries.ShippingCarriersQueries.GetShippingCarriersQuery())).ToList();
        ApplyFilter();
    }

    protected override Task ShowCreateAsync()
    {
        DialogService.Instance.ShowDialog<Create.View>();
        return Task.CompletedTask;
    }

    protected override Task ShowEditAsync(ShippingCarrierListDto model)
    {
        _selectionStore.Id = model.Id;
        DialogService.Instance.ShowDialog<Edit.View>();
        return Task.CompletedTask;
    }

    public IEnumerable<ShippingCarrierListDto> ShippingCarriers { get => _shippingCarriers; private set => SetProperty(ref _shippingCarriers, value); }

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string _searchTerm;

    partial void OnSearchTermChanged(string oldValue, string newValue) => ApplyFilter();

    private void ApplyFilter()
    {
        IEnumerable<ShippingCarrierListDto> filtered = _allShippingCarriers;

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            string term = SearchTerm.Trim();
            filtered = filtered.Where(carrier =>
                carrier.Name?.Contains(term) == true
                || carrier.PhoneNumber?.Contains(term) == true
                || carrier.Address?.Contains(term) == true);
        }

        ShippingCarriers = filtered.ToList();
    }
}
