using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using static OrderHub.Application.DTOs.CommonDtos;

namespace OrderHub.UI.Features.Orders;

public partial class OrderViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    public OrderViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public int Id { get; init; }

    public string OrderNumber { get; init; }

    public string ClientName { get; init; }

    public string ClientPhoneNumber { get; init; }

    public int ItemsCount { get; init; }
    public decimal TotalPrice { get; init; }

    public DateTime CreatedAt { get; init; }

    private OrderStatusInfoDto _orderStatus;
    public OrderStatusInfoDto OrderStatus
    {
        get => _orderStatus;
        set
        {
            if (value is not null && value != _orderStatus)
            {
                SetProperty(ref _orderStatus, value);
                Dispatcher.CurrentDispatcher.Invoke(() => ChangeOrderStatus(value));
            }

            if (_orderStatus.Status == "Cancelled")
                CanEdit = false;
        }
    }

    private bool _canEdit = true;
    public bool CanEdit { get => _canEdit; private set => SetProperty(ref _canEdit, value); }

    //async partial void OnOrderStatusChanged(OrderStatusInfoDto oldValue, OrderStatusInfoDto newValue)
    //{
    //    if (oldValue is not null && oldValue != newValue)
    //    {
    //        await ChangeOrderStatus(newValue);
    //    }
    //}

    private async Task ChangeOrderStatus(OrderStatusInfoDto orderStatusInfoDto)
    {
        await _mediator.Send(new Application.Commands.OrderCommands.ChangeOrderStatusCommand(Id, orderStatusInfoDto.Id));
    }
}