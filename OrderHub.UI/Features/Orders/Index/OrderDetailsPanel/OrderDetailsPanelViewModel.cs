using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using static OrderHub.Application.DTOs.OrderDtos;

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
    private int? _selectedOrderId;

    [ObservableProperty]
    private OrderDetailsDto _selectedOrderDetails;

    async partial void OnSelectedOrderIdChanged(int? oldValue, int? newValue)
    {
        if (newValue.HasValue)
        {
            SelectedOrderDetails = await _mediator.Send(new Application.Queries.OrderQueries.GetOrderDetailsQuery(newValue.Value));
        }
    }
}
