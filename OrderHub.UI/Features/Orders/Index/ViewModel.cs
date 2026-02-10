using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CommonDtos;

namespace OrderHub.UI.Features.Orders.Index;

internal partial class ViewModel : IndexViewModelBase<Order>
{
    private readonly IDialogService _dialogService;

    private IEnumerable<Order> _orders;
    public IEnumerable<Order> Orders { get => _orders; set => SetProperty(ref _orders, value); }

    private IEnumerable<OrderStatusInfoDto> _orderStatuses;
    public IEnumerable<OrderStatusInfoDto> OrderStatuses { get => _orderStatuses; set => SetProperty(ref _orderStatuses, value); }

    [ObservableProperty]
    private bool _isLoading = true;

    public ViewModel(IMediator mediator, IMessenger messenger, IDialogService dialogService) : base(mediator, messenger)
    {
        _dialogService = dialogService;
    }

    protected override Task ShowCreateAsync()
    {
        _dialogService.ShowDialog<Create.View>();
        return Task.CompletedTask;
    }

    protected override async Task LoadAsync()
    {
        IsLoading = true;
        OrderStatuses = await _mediator.Send(new Application.Queries.CommonQueries.GetOrderStautsesInfoQuery());
        Orders = new List<Order>()
        {
            new Order()
            {
                OrderStatus =  OrderStatuses.ElementAt(0),
                OrderNumber = "ORD-20200102-0001",
                ClientName = "Client",
                ContactWay = "Whatsapp",
                CreatedAt = "2020-01-01",
                ItemsQuantity = 5,
                TotalPrice = 20
            },
            new Order()
            {
                OrderStatus = OrderStatuses.ElementAt(0),
                OrderNumber = "ORD-20200102-0002",
                ClientName = "Client 2",
                ContactWay = "Phone",
                CreatedAt = "2020-01-01",
                ItemsQuantity = 7,
                TotalPrice = 50
            },
            new Order()
            {
                OrderStatus = OrderStatuses.ElementAt(3),
                OrderNumber = "ORD-20200102-0003",
                ClientName = "Client 3",
                ContactWay = "Email",
                CreatedAt = "2020-01-01",
                ItemsQuantity = 3,
                TotalPrice = 30
            },
            new Order()
            {
                OrderStatus = OrderStatuses.ElementAt(1),
                OrderNumber = "ORD-20200102-0004",
                ClientName = "Client 4",
                ContactWay = "Phone",
                CreatedAt = "2020-01-01",
                ItemsQuantity = 4,
                TotalPrice = 40
            },
            new Order()
            {
                OrderStatus = OrderStatuses.ElementAt(2),
                OrderNumber = "ORD-20200102-0005",
                ClientName = "Client 5",
                ContactWay = "Email",
                CreatedAt = "2020-01-01",
                ItemsQuantity = 6,
                TotalPrice = 60
            },
            new Order()
            {
                OrderStatus = OrderStatuses.ElementAt(4),
                OrderNumber = "ORD-20200102-0006",
                ClientName = "Client 6",
                ContactWay = "Phone",
                CreatedAt = "2020-01-01",
                ItemsQuantity = 8,
                TotalPrice = 80
            },
            new Order()
            {
                OrderStatus = OrderStatuses.ElementAt(0),
                OrderNumber = "ORD-20200102-0007",
                ClientName = "Client 7",
                ContactWay = "Email",
                CreatedAt = "2020-01-01",
                ItemsQuantity = 9,
                TotalPrice = 90
            }
        };

        OnPropertyChanged(nameof(Orders));
        IsLoading = false;
    }

    protected override async Task ReloadAsync()
    {
        await LoadAsync();
    }

    protected override Task ShowEditAsync(Order model)
    {
        return Task.CompletedTask;
    }

    protected override Task DeleteAsync(Order model)
    {
        throw new System.NotImplementedException();
    }
}
public partial class Order : ObservableObject
{
    [ObservableProperty]
    private string _orderNumber;
    
    [ObservableProperty]
    private string _clientName;
    
    [ObservableProperty]
    private string _contactWay;
    
    [ObservableProperty]
    private int _itemsQuantity;
    
    [ObservableProperty]
    private int _totalPrice;
    
    [ObservableProperty]
    private string _createdAt;
    
    [ObservableProperty]
    private OrderStatusInfoDto _orderStatus;
}