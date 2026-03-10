using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OrderHub.UI.Features.Orders;

public partial class OrderItemViewModel : ObservableObject
{
    public required string ProductName { get; init; }
    public required string CategoryName { get; init; }
    public int ProductId { get; init; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SubTotal))]
    private decimal _price;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SubTotal))]
    private decimal _quantity;

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if(e.PropertyName== nameof(Price) || e.PropertyName == nameof(Quantity))
        {
            OnSubTotalChanged(this, EventArgs.Empty);
        }
    }

    public event EventHandler SubTotalChanged;
    private void OnSubTotalChanged(object sender, EventArgs e) => SubTotalChanged?.Invoke(this, e);

    public decimal SubTotal => Price * Quantity;

    [Required]
    public string SupplierName { get; set; }
    [Required]
    public int? SupplierId { get; set; }

    [ObservableProperty]
    private OrderItemSupplier _supplier;

    partial void OnSupplierChanged(OrderItemSupplier oldValue, OrderItemSupplier newValue)
    {
        SupplierName = newValue?.Name;
        SupplierId = newValue?.Id;
    }

    public IEnumerable<OrderItemSupplier> Suppliers { get; set; } = [];
}

public record OrderItemSupplier(int Id, string Name);