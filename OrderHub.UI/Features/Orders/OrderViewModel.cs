using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CommonDtos;
using static OrderHub.Application.DTOs.PaymentMothodsDtos;

namespace OrderHub.UI.Features.Orders;

public partial class OrderViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    public OrderViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public OrderViewModel(
        IMediator mediator,
        int id,
        string orderNumber,
        string clientName,
        string clientPhoneNumber,
        int itemsCount,
        decimal totalPrice,
        DateTime createdAt,
        PaymentMethodListDto paymentMethodListDto,
        EnumItem<OrderStatus> orderStatus)
    {
        _mediator = mediator;
        Id = id;
        OrderNumber = orderNumber;
        ClientName = clientName;
        ClientPhoneNumber = clientPhoneNumber;
        ItemsCount = itemsCount;
        TotalPrice = totalPrice;
        CreatedAt = createdAt;
        _paymentMethod = paymentMethodListDto;
        _orderStatus = orderStatus;
    }

    public int Id { get; init; }

    public string OrderNumber { get; init; }

    public string ClientName { get; init; }

    public string ClientPhoneNumber { get; init; }

    public int ItemsCount { get; init; }
    public decimal TotalPrice { get; init; }

    [ObservableProperty]
    private IEnumerable<SupplierInfoDto> _suppliers;

    [ObservableProperty]
    private PaymentMethodListDto _paymentMethod;
    async partial void OnPaymentMethodChanged(PaymentMethodListDto oldValue, PaymentMethodListDto newValue)
    {
        if (newValue != null)
            await ChangePaymentMethod(newValue.Id);
    }

    private async Task ChangePaymentMethod(int paymentMethodId)
    {
        await _mediator.Send(new Application.Commands.OrderCommands.ChangePaymentMethodCommand(Id, paymentMethodId));
    }

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