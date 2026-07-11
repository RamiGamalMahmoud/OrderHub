using CommunityToolkit.Mvvm.ComponentModel;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using static OrderHub.Application.DTOs.OrderDtos;
using static OrderHub.Application.DTOs.OrderItemDtos;

namespace OrderHub.UI.Features.Orders.Editor;

public partial class OrderBuilder : ObservableObject
{
    private int _clientId;
    private int? _deliverymanId;
    private int? _shippingCarrierId;
    private IEnumerable<OrderDeliveryStepCreateDto> _deliverySteps = [];

    private DeliveryMethod _deliveryMethod;
    private int? _paymentMethodId;

    public ObservableCollection<OrderItemViewModel> Items { get; } = new();

    [ObservableProperty]
    private decimal _totalPrice;

    public int ItemsCount => Items.Count;

    public OrderBuilder()
    {
        Items.CollectionChanged += (_, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (OrderItemViewModel item in e.NewItems)
                    Attach(item);
            }

            if (e.OldItems != null)
            {
                foreach (OrderItemViewModel item in e.OldItems)
                    Detach(item);
            }

            RecalculateTotals();
            OnPropertyChanged(nameof(ItemsCount));
        };
    }

    private void Attach(OrderItemViewModel item)
    {
        item.PropertyChanged += Item_PropertyChanged;
    }

    private void Detach(OrderItemViewModel item)
    {
        item.PropertyChanged -= Item_PropertyChanged;
    }

    private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OrderItemViewModel.SubTotal))
        {
            RecalculateTotals();
        }
    }

    public void AddItem(OrderItemViewModel item)
    {
        var existing = Items.FirstOrDefault(i =>
            i.ProductId == item.ProductId &&
            i.Price == item.Price);

        if (existing is not null)
        {
            existing.Quantity += item.Quantity;
            return;
        }

        Items.Add(item);
    }

    public void RemoveItem(OrderItemViewModel item)
    {
        Items.Remove(item);
    }

    public void Clear()
    {
        Items.Clear();

    }

    private void RecalculateTotals()
    {
        TotalPrice = Items.Sum(i => i.SubTotal);
    }

    public Result<OrderCreateDto> Build()
    {
        if (!Items.Any())
            return Result<OrderCreateDto>.Failure("Order must contain at least one item.");

        if (_clientId <= 0)
            return Result<OrderCreateDto>.Failure("Client is required.");

        if (_deliveryMethod == DeliveryMethod.DeliveryMan && _deliverymanId is null)
            return Result<OrderCreateDto>.Failure("Deliveryman is required.");

        if (_deliveryMethod == DeliveryMethod.ShippingCompany && _shippingCarrierId is null)
            return Result<OrderCreateDto>.Failure("Shipping carrier is required.");

        if (_deliveryMethod == DeliveryMethod.DeliveryChain && !_deliverySteps.Any())
            return Result<OrderCreateDto>.Failure("At least one delivery step is required.");

        var orderItems = Items.Select(item =>
            new OrderItemDto(
                item.ProductId,
                item.ProductName,
                (int)item.Quantity,
                item.Price,
                item.SupplierName,
                item.SupplierId
            ));

        var deliverySteps = _deliveryMethod == DeliveryMethod.DeliveryChain
            ? _deliverySteps
            : [];

        var dto = new OrderCreateDto(
            _clientId,
            1,
            _deliveryMethod,
            _deliverymanId,
            _shippingCarrierId,
            orderItems,
            deliverySteps,
            _paymentMethodId
        );

        return Result<OrderCreateDto>.Success(dto);
    }

    public OrderBuilder ForClient(int clientId)
    {
        _clientId = clientId;
        return this;
    }

    public OrderBuilder WithDeliveryMethod(DeliveryMethod method)
    {
        _deliveryMethod = method;
        return this;
    }

    public OrderBuilder WithDeliveryman(int? id)
    {
        _deliverymanId = id;
        return this;
    }

    public OrderBuilder WithShippingCarrier(int? id)
    {
        _shippingCarrierId = id;
        return this;
    }

    public OrderBuilder WithPaymentMethod(int? id)
    {
        _paymentMethodId = id;
        return this;
    }

    public OrderBuilder WithDeliverySteps(IEnumerable<OrderDeliveryStepCreateDto> steps)
    {
        _deliverySteps = steps ?? [];
        return this;
    }
}
