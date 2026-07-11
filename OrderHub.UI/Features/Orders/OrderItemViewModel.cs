using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OrderHub.UI.Features.Orders;

public partial class OrderItemViewModel : ObservableValidator
{
    private readonly IMediator _mediator;

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

    public IEnumerable<OrderItemSupplier> Suppliers { get; init; } = [];
    public bool IsValid => !HasErrors;

    public OrderItemViewModel(IMediator mediator)
    {
        _mediator = mediator;
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
