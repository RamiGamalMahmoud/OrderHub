using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace OrderHub.UI.Features.Orders.Editor.Services;

public interface IOrderBuilder
{
    ObservableCollection<OrderItem> Items { get; }
    decimal TotalPrice { get; }
    int Count { get; }

    event EventHandler ItemsChanged;

    void AddItem(OrderItem item);
    void RemoveItem(OrderItem item);
    void UpdateItemPrice(OrderItem item, decimal newPrice);
    void UpdateItemQuantity(OrderItem item, decimal newQuantity);
    void Clear();
}

public partial class OrderBuilder : ObservableObject, IOrderBuilder
{
    [ObservableProperty]
    private decimal _totalPrice;

    [ObservableProperty]
    private int _count;

    public ObservableCollection<OrderItem> Items { get; } = new();

    public event EventHandler ItemsChanged;

    public OrderBuilder()
    {
        Items.CollectionChanged += (s, e) =>
        {
            RecalculateTotals();
            ItemsChanged?.Invoke(this, EventArgs.Empty);
        };
    }

    public void AddItem(OrderItem item)
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
                if (e.PropertyName == nameof(OrderItem.SubTotal))
                    RecalculateTotals();
            };

            item.SubTotalChanged += (s, e) => RecalculateTotals();
        }
    }

    public void RemoveItem(OrderItem item)
    {
        Items.Remove(item);
        item.SubTotalChanged -= (s, e) => RecalculateTotals();
    }

    public void UpdateItemPrice(OrderItem item, decimal newPrice)
    {
        if (Items.Contains(item))
            item.Price = newPrice;
    }

    public void UpdateItemQuantity(OrderItem item, decimal newQuantity)
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
}