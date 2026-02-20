using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CommonDtos;
using static OrderHub.Application.DTOs.OrderDtos;

namespace OrderHub.UI.Features.Orders.Index;

internal partial class ViewModel : IndexViewModelBase<OrderViewModel>
{
    private readonly IDialogService _dialogService;

    private ObservableCollection<OrderViewModel> _orders = [];
    public IEnumerable<OrderViewModel> Orders => _orders;

    private IEnumerable<OrderStatusInfoDto> _orderStatuses;
    public IEnumerable<OrderStatusInfoDto> OrderStatuses { get => _orderStatuses; set => SetProperty(ref _orderStatuses, value); }

    [ObservableProperty]
    private bool _isLoading = true;

    public ViewModel(IMediator mediator, IMessenger messenger, IDialogService dialogService) : base(mediator, messenger)
    {
        _dialogService = dialogService;

        messenger.Register<Application.Messages.Orders.OrderCreatedMessage>(this, async (r, m) => await ReloadAsync());
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

        IEnumerable<OrderListDto> orderList = await _mediator.Send(new Application.Queries.OrderQueries.GetOrdersQuery());

        _orders = new ObservableCollection<OrderViewModel>( orderList.Select(o => new OrderViewModel(_mediator)
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            ClientName = o.ClientName,
            ClientPhoneNumber = o.ClientPhoneNumber,
            ItemsCount = o.ItemsCount,
            TotalPrice = o.Total,
            CreatedAt = o.CreatedAt,
            OrderStatus = OrderStatuses.Where(os => os.Id == o.OrderStatusInfoDto.Id).SingleOrDefault()
        }));

        OnPropertyChanged(nameof(Orders));
        IsLoading = false;
    }

    protected override async Task ReloadAsync()
    {
        await LoadAsync();
    }

    protected override Task ShowEditAsync(OrderViewModel model)
    {
        return Task.CompletedTask;
    }

    protected override Task DeleteAsync(OrderViewModel model)
    {
        throw new System.NotImplementedException();
    }

    [ObservableProperty]
    private string _searchTerm;
    async partial void OnSearchTermChanged(string oldValue, string newValue)
    {
        IEnumerable<OrderListDto> orderList = await _mediator.Send(new Application.Queries.OrderQueries.GetClientOrdersQuery(newValue));
        _orders = new ObservableCollection<OrderViewModel>(orderList.Select(o => new OrderViewModel(_mediator)
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            ClientName = o.ClientName,
            ClientPhoneNumber = o.ClientPhoneNumber,
            ItemsCount = o.ItemsCount,
            TotalPrice = o.Total,
            CreatedAt = o.CreatedAt,
            OrderStatus = OrderStatuses.Where(os => os.Id == o.OrderStatusInfoDto.Id).SingleOrDefault()
        }));

        OnPropertyChanged(nameof(Orders));
        IsLoading = false;
    }
}
