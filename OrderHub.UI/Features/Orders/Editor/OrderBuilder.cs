using CommunityToolkit.Mvvm.ComponentModel;
using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using static OrderHub.Application.DTOs.OrderDtos;
using static OrderHub.Application.DTOs.OrderItemDtos;

namespace OrderHub.UI.Features.Orders.Editor;

public partial class OrderBuilder : ObservableObject
{

    private int _clientId;
    private int? _deliverymanId;
    private int? _shippingCarrierId;

    [ObservableProperty]
    private decimal _totalPrice;

    public int ItemsCount => Items.Count;
    private DeliveryMethod _deliveryMethod;

    public ObservableCollection<OrderItemViewModel> Items { get; } = new();

    public event EventHandler ItemsChanged;

    public OrderBuilder()
    {
        Items.CollectionChanged += (s, e) =>
        {
            RecalculateTotals();
            ItemsChanged?.Invoke(this, EventArgs.Empty);
            OnPropertyChanged(nameof(ItemsCount));
        };
    }

    public void AddItem(OrderItemViewModel item)
    {
        // Check for existing product to merge quantities
        var existing = Items.FirstOrDefault(i =>
            i.ProductName == item.ProductName &&
            i.Price == item.Price);

        if (existing is not null)
        {
            existing.Quantity += item.Quantity;
        }
        else
        {
            Items.Add(item);
            // Hook up property changes for recalculation
            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(OrderItemViewModel.SubTotal))
                    RecalculateTotals();
            };

            item.SubTotalChanged += (s, e) => RecalculateTotals();
        }
    }

    public void RemoveItem(OrderItemViewModel item)
    {
        Items.Remove(item);
        item.SubTotalChanged -= (s, e) => RecalculateTotals();
    }

    public void UpdateItemPrice(OrderItemViewModel item, decimal newPrice)
    {
        if (Items.Contains(item))
            item.Price = newPrice;
    }

    public void UpdateItemQuantity(OrderItemViewModel item, decimal newQuantity)
    {
        if (Items.Contains(item))
            item.Quantity = newQuantity;
    }

    public void Clear()
    {
        Items.Clear();
    }

    private void RecalculateTotals()
    {
        TotalPrice = Items.Sum(i => i.SubTotal);
    }

    public OrderCreateDto Build()
    {
        if (_deliveryMethod is DeliveryMethod.DeliveryMan && _deliverymanId is null)
        {
            throw new Exception("Deliveryman is required");
        }

        if (_deliveryMethod is DeliveryMethod.ShippingCompany && _shippingCarrierId is null)
        {
            throw new Exception("Shipping carrier is required");
        }

        IEnumerable<OrderItemDto> orderItems = Items.Select(item => new OrderItemDto(item.ProductId, item.ProductName, (int)item.Quantity, item.Price));
        OrderCreateDto orderCreateDto = new OrderCreateDto(_clientId, 1, _deliveryMethod, _deliverymanId, _shippingCarrierId, orderItems);
        return orderCreateDto;
    }

    public OrderBuilder WithDeliveryMethod(DeliveryMethod deliveryMethod)
    {
        _deliveryMethod = deliveryMethod;
        return this;
    }

    public OrderBuilder WithShippingCarrier(int? shippingCarrierId)
    {
        _shippingCarrierId = shippingCarrierId;
        return this;
    }

    public OrderBuilder WithDeliveryman(int? deliverymanId)
    {
        _deliverymanId = deliverymanId;
        return this;
    }

    public OrderBuilder ForClient(int clientId)
    {
        _clientId = clientId;
        return this;
    }
}