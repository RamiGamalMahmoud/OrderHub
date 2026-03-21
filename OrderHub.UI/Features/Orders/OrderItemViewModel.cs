using CommunityToolkit.Mvvm.ComponentModel;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OrderHub.UI.Features.Orders;

public partial class OrderItemViewModel : ObservableValidator
{
    // =========================
    // 📦 Static Data
    // =========================

    public required string ProductName { get; init; }
    public required string CategoryName { get; init; }
    public int ProductId { get; init; }

    // =========================
    // 💰 Pricing
    // =========================

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SubTotal))]
    private decimal _price;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SubTotal))]
    private decimal _quantity;

    public decimal SubTotal => Price * Quantity;

    // =========================
    // 🏢 Supplier
    // =========================

    [ObservableProperty]
    private OrderItemSupplier _supplier;

    [ObservableProperty]
    [Required(ErrorMessage = "Supplier is required")]
    private string _supplierName;

    [ObservableProperty]
    [Required(ErrorMessage = "Supplier must be selected")]
    private int? _supplierId;

    partial void OnSupplierChanged(OrderItemSupplier oldValue, OrderItemSupplier newValue)
    {
        SupplierName = newValue?.Name;
        SupplierId = newValue?.Id;

        ValidateAllProperties();
    }

    // =========================
    // 📋 Data Source
    // =========================

    public IEnumerable<OrderItemSupplier> Suppliers { get; init; } = [];

    // =========================
    // 🧪 Validation API
    // =========================

    public bool IsValid => !HasErrors;
}

public record OrderItemSupplier(int Id, string Name);