using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OrderHub.UI.Features.Orders;

public partial class OrderItemViewModel : ObservableValidator
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

    public decimal SubTotal => Price * Quantity;

    [ObservableProperty]
    private OrderItemSupplier _supplier;

    partial void OnSupplierChanged(OrderItemSupplier value)
    {
        if (value is null)
            return;
        SupplierName = value.Name;
        SupplierId = value.Id;
    }

    [ObservableProperty]
    [Required(ErrorMessage = "Supplier is required")]
    private string _supplierName;

    [ObservableProperty]
    [Required(ErrorMessage = "Supplier must be selected")]
    private int? _supplierId;

    public IEnumerable<OrderItemSupplier> Suppliers { get; init; } = [];
    public bool IsValid => !HasErrors;
}

public record OrderItemSupplier(int Id, string Name);
public record OrderItemProperty();
