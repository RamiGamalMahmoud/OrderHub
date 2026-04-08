using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace OrderHub.UI.Features.Orders;

public partial class OrderItemViewModel : ObservableValidator
{
    private static readonly string[] _helperFieldKeys =
    [
        "القماش",
        "الخشب",
        "الموديل",
        "اللون",
        "المقاس"
    ];

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

    [ObservableProperty]
    [Required(ErrorMessage = "Supplier is required")]
    private string _supplierName;

    [ObservableProperty]
    [Required(ErrorMessage = "Supplier must be selected")]
    private int? _supplierId;

    [ObservableProperty]
    private string _selectedHelperField;

    [ObservableProperty]
    private string _customFieldName;

    [ObservableProperty]
    private string _customFieldValue;

    [ObservableProperty]
    private string _selectedHelperFieldValue;

    public ObservableCollection<OrderItemAttributeViewModel> Attributes { get; } = [];

    partial void OnSupplierChanged(OrderItemSupplier oldValue, OrderItemSupplier newValue)
    {
        SupplierName = newValue?.Name;
        SupplierId = newValue?.Id;

        ValidateAllProperties();
    }

    public IEnumerable<OrderItemSupplier> Suppliers { get; init; } = [];
    public IEnumerable<string> SuggestedFields => _helperFieldKeys.Except(Attributes.Select(attribute => attribute.Name), StringComparer.OrdinalIgnoreCase);
    public bool IsValid => !HasErrors;

    public OrderItemViewModel()
    {
        Attributes.CollectionChanged += Attributes_CollectionChanged;
    }

    public void LoadAttributes(IEnumerable<OrderItemAttributeViewModel> attributes)
    {
        Attributes.Clear();
        foreach (OrderItemAttributeViewModel attribute in attributes ?? Enumerable.Empty<OrderItemAttributeViewModel>())
        {
            Attributes.Add(attribute);
        }
    }

    [RelayCommand]
    private void AddSuggestedField()
    {
        if (string.IsNullOrWhiteSpace(SelectedHelperField))
        {
            return;
        }

        AddAttribute(SelectedHelperField.Trim(), SelectedHelperFieldValue?.ToString() ?? string.Empty);
        SelectedHelperField = null;
        SelectedHelperFieldValue = null;
    }

    [RelayCommand]
    private void AddCustomField()
    {
        if (string.IsNullOrWhiteSpace(CustomFieldName))
        {
            return;
        }

        AddAttribute(CustomFieldName.Trim(), CustomFieldValue?.Trim() ?? string.Empty);
        CustomFieldName = string.Empty;
        CustomFieldValue = string.Empty;
    }

    [RelayCommand]
    private void RemoveAttribute(OrderItemAttributeViewModel attribute)
    {
        if (attribute is null)
        {
            return;
        }

        attribute.PropertyChanged -= Attribute_PropertyChanged;
        Attributes.Remove(attribute);
        OnPropertyChanged(nameof(SuggestedFields));
    }

    private void AddAttribute(string name, string value)
    {
        if (Attributes.Any(attribute => string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        OrderItemAttributeViewModel attribute = new()
        {
            Name = name,
            Value = value
        };

        attribute.PropertyChanged += Attribute_PropertyChanged;
        Attributes.Add(attribute);
        OnPropertyChanged(nameof(SuggestedFields));
    }

    private void Attributes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (OrderItemAttributeViewModel attribute in e.NewItems)
            {
                attribute.PropertyChanged += Attribute_PropertyChanged;
            }
        }

        if (e.OldItems is not null)
        {
            foreach (OrderItemAttributeViewModel attribute in e.OldItems)
            {
                attribute.PropertyChanged -= Attribute_PropertyChanged;
            }
        }

        OnPropertyChanged(nameof(SuggestedFields));
        OnPropertyChanged(nameof(Attributes));
    }

    private void Attribute_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Attributes));
        OnPropertyChanged(nameof(SuggestedFields));
    }
}

public partial class OrderItemAttributeViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _value;
}

public record OrderItemSupplier(int Id, string Name);
