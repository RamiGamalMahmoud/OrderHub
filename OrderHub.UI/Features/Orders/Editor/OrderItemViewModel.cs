using CommunityToolkit.Mvvm.ComponentModel;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Services.Pricing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

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
    private int _quantity;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SubTotal))]
    decimal _vAT;

    [ObservableProperty]
    private PricingItemResult _pricing;

    public decimal SubTotal
    {
        get
        {
            try
            {
                Pricing = PricingCalculator.CalculateItem(new PricingItem(
                    ProductId,
                    ProductName,
                    Quantity,
                    Price,
                    VAT));
                return Pricing.Total;
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }

    [ObservableProperty]
    private OrderItemSupplier _supplier;

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        StateChanged?.Invoke(this, e);
    }

    private IReadOnlyList<OrderItemProperty> _properties;

    public IReadOnlyList<OrderItemProperty> Properties
    {
        get => _properties;

        init
        {
            _properties = value;

            foreach(var property in value)
            {
                property.StateChanged += Property_StateChanged;
            }
        }
    }

    private void Property_StateChanged(object sender, EventArgs e)
    {
        StateChanged?.Invoke(this, e);
        OnPropertyChanged(nameof(IsValid));
    }

    public event EventHandler StateChanged;

    public IEnumerable<OrderItemSupplier> Suppliers { get; init; } = [];
    public bool IsValid => !HasErrors && Properties.All(p => !p.HasErrors);
}

public record OrderItemSupplier(int Id, string Name);

public partial class OrderItemProperty : ObservableValidator
{
    public int Id { get; init; }
    public string Name { get; init; }
    public bool IsRequired { get; init; }
    public PropertyType PropertyType { get; init; }
    public IEnumerable<OrderItemPropertyOption> Options { get; init; }

    public event EventHandler StateChanged;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(OrderItemProperty), nameof(ValidateRequiredValue), ErrorMessage = "Â–Â «·Œ«’Ì… „ÿ·Ê»…")]
    private string _value;

    public OrderItemProperty(
        int id,
        string name,
        bool isRequired,
        PropertyType propertyType,
        IReadOnlyList<OrderItemPropertyOption> options,
        string value = null)
    {
        Id = id;
        Name = name;
        IsRequired = isRequired;
        PropertyType = propertyType;
        Options = options;
        Value = value;
        ErrorsChanged += (_, _) => StateChanged?.Invoke(this, EventArgs.Empty);
        ValidateAllProperties();
    }

    partial void OnValueChanged(string oldValue, string newValue)
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public static ValidationResult ValidateRequiredValue(string value, ValidationContext context)
    {
        OrderItemProperty orderItemProperty = context.ObjectInstance as OrderItemProperty;

        if (orderItemProperty.IsRequired && string.IsNullOrEmpty(value) && orderItemProperty.PropertyType != PropertyType.Boolean)
            return new ValidationResult("");
        return ValidationResult.Success;
    }
}
public record OrderItemPropertyOption(int Id, string Value);
