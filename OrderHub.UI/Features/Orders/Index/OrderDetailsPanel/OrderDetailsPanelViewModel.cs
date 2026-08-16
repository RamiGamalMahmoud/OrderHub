using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using OrderHub.Application.Features.Orders.Queries;
using OrderHub.UI.Interfaces;
using System.Diagnostics;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Orders.Index.OrderDetailsPanel;

public partial class OrderDetailsPanelViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IDialogService _dialogService;

    public OrderDetailsPanelViewModel(IMediator mediator, IDialogService dialogService)
    {
        _mediator = mediator;
        _dialogService = dialogService;
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
