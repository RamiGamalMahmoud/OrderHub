using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using static OrderHub.Application.DTOs.ClientDtos;
using static OrderHub.Application.DTOs.OrderDtos;
using static OrderHub.Application.DTOs.OrderItemDtos;

namespace OrderHub.UI.Features.Orders.Editor;

public interface IOrderBuilder
{
    ObservableCollection<OrderItemViewModel> Items { get; }
    decimal TotalPrice { get; }
    int Count { get; }

    event EventHandler ItemsChanged;

    void AddItem(OrderItemViewModel item);
    void RemoveItem(OrderItemViewModel item);
    void UpdateItemPrice(OrderItemViewModel item, decimal newPrice);
    void UpdateItemQuantity(OrderItemViewModel item, decimal newQuantity);
    void Clear();
}

public partial class OrderBuilder : ObservableObject, IOrderBuilder
{
    private static int _orderNumber = 1;
    [ObservableProperty]
    private decimal _totalPrice;

    [ObservableProperty]
    private int _count;

    public ObservableCollection<OrderItemViewModel> Items { get; } = new();

    public event EventHandler ItemsChanged;

    public OrderBuilder()
    {
        Items.CollectionChanged += (s, e) =>
        {
            RecalculateTotals();
            ItemsChanged?.Invoke(this, EventArgs.Empty);
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
        Count = Items.Count;
    }

    public OrderCreateDto Build(ClientListDto client)
    {
        IEnumerable<OrderItemDto> orderItems = Items.Select(item => new OrderItemDto(item.ProductId, item.ProductName, (int) item.Quantity, item.Price));
        OrderCreateDto orderCreateDto = new OrderCreateDto(client.Id, 1, CreateOrderNumber(), orderItems);
        return orderCreateDto;
    }

    private static string CreateOrderNumber()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("ORD-");
        sb.Append(DateTime.Now.ToString("yyyyMMdd"));
        sb.Append('-');
        sb.Append(_orderNumber++.ToString().PadLeft(4, '0'));
        return sb.ToString();
    }
}