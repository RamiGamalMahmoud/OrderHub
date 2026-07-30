using CommunityToolkit.Mvvm.ComponentModel;
using OrderHub.Domain.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OrderHub.UI.Features.Orders;

public partial class OrderItemViewModel : ObservableValidator
{
    public required string ProductName { get; init; }
    public required string CategoryName { get; init; }
    public required int ProductId { get; init; }

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

    [ObservableProperty]
    private IEnumerable<OrderItemProperty> _properties;

    public IEnumerable<OrderItemSupplier> Suppliers { get; init; } = [];
    public bool IsValid => !HasErrors;
}

public record OrderItemSupplier(int Id, string Name);
public partial class OrderItemProperty : ObservableObject
{
        public int Id {get; init; }
        public string Name {get; init; }
        public bool IsRequired {get; init; }
        public PropertyType PropertyType {get; init; }
        public IEnumerable<OrderItemPropertyOption> Options { get; init; }

    [ObservableProperty]
    private string _selectedOptionValue;

    partial void OnSelectedOptionValueChanged(string oldValue, string newValue)
    {
        
    }

    public OrderItemProperty(int id,
                             string name,
                             bool isRequired,
                             PropertyType propertyType,
                             IEnumerable<OrderItemPropertyOption> options)
    {
        Id = id;
        Name = name;
        IsRequired = isRequired;
        PropertyType = propertyType;
        Options = options;
    }
}
public record OrderItemPropertyOption(int Id, string Value);
