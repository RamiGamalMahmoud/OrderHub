using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.DeliverymanDtos;
using static OrderHub.Application.DTOs.ShippingCarriersDtos;

namespace OrderHub.UI.Features.Orders.Editor;

internal partial class DeliveryPartyLookupViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private CancellationTokenSource _deliverymanSearchCts;
    private CancellationTokenSource _shippingCarrierSearchCts;

    public DeliveryPartyLookupViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    [ObservableProperty]
    private ShippingCarrierListDto _selectedShippingCarrier;

    [ObservableProperty]
    private DeliverymanListDto _selectedDeliveryman;

    [ObservableProperty]
    private IEnumerable<DeliverymanListDto> _deliverymen = [];

    [ObservableProperty]
    private IEnumerable<ShippingCarrierListDto> _shippingCarriers = [];

    [ObservableProperty]
    private string _deliverymanSearchTerm;

    [ObservableProperty]
    private string _shippingCarrierSearchTerm;

    public void ClearSelections()
    {
        SelectedDeliveryman = null;
        SelectedShippingCarrier = null;
    }

    public void SetDeliverymen(IEnumerable<DeliverymanListDto> deliverymen)
    {
        Deliverymen = MergeSelectedItem(deliverymen, SelectedDeliveryman);
    }

    public void SetShippingCarriers(IEnumerable<ShippingCarrierListDto> shippingCarriers)
    {
        ShippingCarriers = MergeSelectedItem(shippingCarriers, SelectedShippingCarrier);
    }

    async partial void OnDeliverymanSearchTermChanged(string oldValue, string newValue)
    {
        _deliverymanSearchCts?.Cancel();
        _deliverymanSearchCts = new CancellationTokenSource();
        CancellationToken token = _deliverymanSearchCts.Token;

        try
        {
            await Task.Delay(300, token);
            IEnumerable<DeliverymanListDto> deliverymen = await _mediator.Send(
                new Application.Queries.DeliverymanQueries.GetDeliverymenByNameQuery(newValue),
                token);
            SetDeliverymen(deliverymen);
        }
        catch (OperationCanceledException)
        {
        }
    }

    async partial void OnShippingCarrierSearchTermChanged(string oldValue, string newValue)
    {
        _shippingCarrierSearchCts?.Cancel();
        _shippingCarrierSearchCts = new CancellationTokenSource();
        CancellationToken token = _shippingCarrierSearchCts.Token;

        try
        {
            await Task.Delay(300, token);
            IEnumerable<ShippingCarrierListDto> shippingCarriers = await _mediator.Send(
                new Application.Queries.ShippingCarriersQueries.GetShippingCarriersByNameQuery(newValue),
                token);
            SetShippingCarriers(shippingCarriers);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static IEnumerable<TItem> MergeSelectedItem<TItem>(IEnumerable<TItem> items, TItem selectedItem)
        where TItem : class
    {
        TItem[] results = (items ?? Enumerable.Empty<TItem>()).ToArray();

        if (selectedItem is null || results.Contains(selectedItem))
            return results;

        return new[] { selectedItem }.Concat(results);
    }
}
