using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
    private ShippingCarrier _selectedShippingCarrier;

    [ObservableProperty]
    private Deliveryman _selectedDeliveryman;

    [ObservableProperty]
    private IEnumerable<Deliveryman> _deliverymen = [];

    [ObservableProperty]
    private IEnumerable<ShippingCarrier> _shippingCarriers = [];

    [ObservableProperty]
    private string _deliverymanSearchTerm;

    [ObservableProperty]
    private string _shippingCarrierSearchTerm;

    public void ClearSelections()
    {
        SelectedDeliveryman = null;
        SelectedShippingCarrier = null;
    }

    public void SetDeliverymen(IEnumerable<Deliveryman> deliverymen)
    {
        Deliverymen = MergeSelectedItem(deliverymen, SelectedDeliveryman);
    }

    public void SetShippingCarriers(IEnumerable<ShippingCarrier> shippingCarriers)
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

            IEnumerable<Deliveryman> deliverymen =
                (await _mediator.Send(
                    new Application.Queries.DeliverymanQueries.GetDeliverymenByNameQuery(newValue),
                    token))
                .Select(x => new Deliveryman(
                    x.Id,
                    x.Name,
                    x.CityName,
                    x.PhoneNumber,
                    x.WhatsappGroupName));

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

            IEnumerable<ShippingCarrier> shippingCarriers =
                (await _mediator.Send(
                    new Application.Queries.ShippingCarriersQueries.GetShippingCarriersByNameQuery(newValue),
                    token))
                .Select(x => new ShippingCarrier(
                    x.Id,
                    x.Name,
                    x.ShippingCost,
                    x.PhoneNumber,
                    x.Address));

            SetShippingCarriers(shippingCarriers);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static IEnumerable<TItem> MergeSelectedItem<TItem>(
        IEnumerable<TItem> items,
        TItem selectedItem)
        where TItem : class
    {
        TItem[] results = (items ?? Enumerable.Empty<TItem>()).ToArray();

        if (selectedItem is null || results.Contains(selectedItem))
            return results;

        return new[] { selectedItem }.Concat(results);
    }
}