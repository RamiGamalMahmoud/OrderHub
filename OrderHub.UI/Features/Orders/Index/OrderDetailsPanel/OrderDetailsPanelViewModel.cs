using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using OrderHub.Application.Features.Orders.Queries;

namespace OrderHub.UI.Features.Orders.Index.OrderDetailsPanel;

public partial class OrderDetailsPanelViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    public OrderDetailsPanelViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    [ObservableProperty]
    private OrderViewModel _selectedOrder;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedOrder))]
    private int? _selectedOrderId;

    [ObservableProperty]
    private GetOrderDetials.Order _selectedOrderDetails;

    public bool HasSelectedOrder => SelectedOrderId != null;

    async partial void OnSelectedOrderIdChanged(int? oldValue, int? newValue)
    {
        if (newValue.HasValue)
        {
            SelectedOrderDetails = await _mediator.Send(new GetOrderDetials.Query(newValue.Value));
        }
    }
}
