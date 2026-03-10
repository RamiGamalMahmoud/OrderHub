using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    [ObservableProperty]
    private IEnumerable<SupplierInfoDto> _suppliers;

    public DateTime CreatedAt { get; init; }

    [ObservableProperty]
    private EnumItem<OrderStatus> _orderStatus;
    async partial void OnOrderStatusChanged(EnumItem<OrderStatus> oldValue, EnumItem<OrderStatus> newValue)
    {
        await ChangeOrderStatus(newValue.Value);
    }

    public IEnumerable<EnumItem<OrderStatus>> OrderStatuses =>
        Enum.GetValues<OrderStatus>()
        .Cast<OrderStatus>()
        .Select(e => new EnumItem<OrderStatus>(e, e.GetDescription()));

    private bool _canEdit = true;
    public bool CanEdit { get => _canEdit; private set => SetProperty(ref _canEdit, value); }

    private async Task ChangeOrderStatus(OrderStatus orderStatus)
    {
        await _mediator.Send(new Application.Commands.OrderCommands.ChangeOrderStatusCommand(Id, orderStatus));
    }
}