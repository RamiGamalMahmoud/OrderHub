using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Application.Common.Lookups;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Orders;

public partial class OrderViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IMessenger _messenger;

    public OrderViewModel(IMediator mediator, IMessenger messenger)
    {
        _mediator = mediator;
        _messenger = messenger;
    }

    public OrderViewModel(
        IMediator mediator,
        IMessenger messenger,
        int id,
        string orderNumber,
        string clientName,
        string clientPhoneNumber,
        int itemsCount,
        decimal totalPrice,
        DateTime createdAt,
        PaymentMethod paymentMethodListDto,
        EnumItem<OrderStatus> orderStatus,
        bool hasClientRecipient,
        bool hasSupplierRecipient,
        bool hasShippingCarrierRecipient,
        bool hasDeliverymanRecipient,
        bool isClientMessageSent,
        bool isSupplierMessageSent,
        bool isShippingCarrierMessageSent,
        bool isDeliverymanMessageSent)
    {
        _mediator = mediator;
        _messenger = messenger;
        Id = id;
        OrderNumber = orderNumber;
        ClientName = clientName;
        ClientPhoneNumber = clientPhoneNumber;
        ItemsCount = itemsCount;
        TotalPrice = totalPrice;
        CreatedAt = createdAt;
        _paymentMethod = paymentMethodListDto;
        _orderStatus = orderStatus;
        HasClientRecipient = hasClientRecipient;
        HasSupplierRecipient = hasSupplierRecipient;
        HasShippingCarrierRecipient = hasShippingCarrierRecipient;
        HasDeliverymanRecipient = hasDeliverymanRecipient;
        IsClientMessageSent = isClientMessageSent;
        IsSupplierMessageSent = isSupplierMessageSent;
        IsShippingCarrierMessageSent = isShippingCarrierMessageSent;
        IsDeliverymanMessageSent = isDeliverymanMessageSent;
    }

    public int Id { get; init; }

    public string OrderNumber { get; init; }

    public string ClientName { get; init; }

    public string ClientPhoneNumber { get; init; }

    public int ItemsCount { get; init; }
    public decimal TotalPrice { get; init; }

    [ObservableProperty]
    private IEnumerable<Supplier> _suppliers;

    [ObservableProperty]
    private PaymentMethod _paymentMethod;

    partial void OnPaymentMethodChanged(PaymentMethod value)
    {
        if (value != null)
        {
            _ = ChangePaymentMethod(value.Id);
        }
    }

    private async Task ChangePaymentMethod(int paymentMethodId)
    {
        Result result = await _mediator.Send(
            new Application.Commands.OrderCommands.ChangePaymentMethodCommand(Id, paymentMethodId));

        if (result.IsSuccess)
        {
            _messenger.Send(new Application.Messages.Orders.OrderUpdatedMessage());
        }
    }

    public DateTime CreatedAt { get; init; }

    [ObservableProperty]
    private bool _hasClientRecipient;

    [ObservableProperty]
    private bool _hasSupplierRecipient;

    [ObservableProperty]
    private bool _hasShippingCarrierRecipient;

    [ObservableProperty]
    private bool _hasDeliverymanRecipient;

    [ObservableProperty]
    private bool _isClientMessageSent;

    [ObservableProperty]
    private bool _isSupplierMessageSent;

    [ObservableProperty]
    private bool _isShippingCarrierMessageSent;

    [ObservableProperty]
    private bool _isDeliverymanMessageSent;

    public bool IsAllMessageSent =>
        (!HasClientRecipient || IsClientMessageSent)
        && (!HasSupplierRecipient || IsSupplierMessageSent)
        && (!HasShippingCarrierRecipient || IsShippingCarrierMessageSent)
        && (!HasDeliverymanRecipient || IsDeliverymanMessageSent);

    public bool CanBroadcastAll =>
        HasClientRecipient || HasSupplierRecipient || HasShippingCarrierRecipient || HasDeliverymanRecipient;

    [ObservableProperty]
    private EnumItem<OrderStatus> _orderStatus;

    async partial void OnOrderStatusChanged(EnumItem<OrderStatus> oldValue, EnumItem<OrderStatus> newValue)
    {
        await ChangeOrderStatus(newValue.Value);
    }

    public IEnumerable<EnumItem<OrderStatus>> OrderStatuses => EnumItems.For<OrderStatus>();

    private bool _canEdit = true;
    public bool CanEdit { get => _canEdit; private set => SetProperty(ref _canEdit, value); }

    private async Task ChangeOrderStatus(OrderStatus orderStatus)
    {
        Result result = await _mediator.Send(
            new Application.Commands.OrderCommands.ChangeOrderStatusCommand(Id, orderStatus));

        if (result.IsSuccess)
        {
            _messenger.Send(new Application.Messages.Orders.OrderUpdatedMessage());
        }
    }

    partial void OnHasClientRecipientChanged(bool oldValue, bool newValue)
    {
        OnPropertyChanged(nameof(CanBroadcastAll));
        OnPropertyChanged(nameof(IsAllMessageSent));
    }

    partial void OnHasSupplierRecipientChanged(bool oldValue, bool newValue)
    {
        OnPropertyChanged(nameof(CanBroadcastAll));
        OnPropertyChanged(nameof(IsAllMessageSent));
    }

    partial void OnHasShippingCarrierRecipientChanged(bool oldValue, bool newValue)
    {
        OnPropertyChanged(nameof(CanBroadcastAll));
        OnPropertyChanged(nameof(IsAllMessageSent));
    }

    partial void OnHasDeliverymanRecipientChanged(bool oldValue, bool newValue)
    {
        OnPropertyChanged(nameof(CanBroadcastAll));
        OnPropertyChanged(nameof(IsAllMessageSent));
    }

    partial void OnIsClientMessageSentChanged(bool oldValue, bool newValue) => OnPropertyChanged(nameof(IsAllMessageSent));
    partial void OnIsSupplierMessageSentChanged(bool oldValue, bool newValue) => OnPropertyChanged(nameof(IsAllMessageSent));
    partial void OnIsShippingCarrierMessageSentChanged(bool oldValue, bool newValue) => OnPropertyChanged(nameof(IsAllMessageSent));
    partial void OnIsDeliverymanMessageSentChanged(bool oldValue, bool newValue) => OnPropertyChanged(nameof(IsAllMessageSent));

    public void UpdateRecipientStatus(RecipientType recipientType, bool isSent)
    {
        switch (recipientType)
        {
            case RecipientType.Client:
                IsClientMessageSent = isSent;
                break;
            case RecipientType.Supplier:
                IsSupplierMessageSent = isSent;
                break;
            case RecipientType.ShippingCarrier:
                IsShippingCarrierMessageSent = isSent;
                break;
            case RecipientType.Deliveryman:
                IsDeliverymanMessageSent = isSent;
                break;
        }
    }
}

public record Supplier(int Id, string Name);
public record PaymentMethod(int Id, string DisplayName, string Description, bool IsActive);