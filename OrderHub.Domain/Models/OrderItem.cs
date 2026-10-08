using OrderHub.Domain.Common;
using OrderHub.Domain.ValueObjects;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OrderHub.Domain.Models;

public class OrderItem : ModelBase
{
    private OrderItem()
    {
    }

    private OrderItem(
        int productId,
        string productName,
        decimal unitPrice,
        int quantity,
        decimal vatRate,
        string supplierName,
        int? supplierId,
        IReadOnlyCollection<OrderItemPropertyData> properties)
    {
        ProductId = productId;
        ProductName = productName;
        UnitPrice = new Money(unitPrice);
        Quantity = quantity;
        VatRate = vatRate;
        SupplierName = supplierName;
        SupplierId = supplierId;

        foreach (var property in properties)
        {
            _properties.Add(new OrderItemProperty()
            {
                PropertyId = property.PropertyId,
                Value = property.Value,
            });
        }
    }

    private readonly Collection<OrderItemProperty> _properties = [];
    public IReadOnlyCollection<OrderItemProperty> Properties => new ReadOnlyCollection<OrderItemProperty>(_properties);
    public int ProductId { get; private set; }
    public Product Product { get; private set; }

    public int OrderId { get; private set; }
    public Order Order { get; private set; }

    public string ProductName { get; private set; }

    public int? SupplierId { get; private set; }
    public Supplier Supplier { get; private set; }

    public string SupplierName { get; private set; }

    public Money UnitPrice { get; private set; }

    public int Quantity { get; private set; }
    public decimal VatRate { get; private set; }

    public Money SubTotal => UnitPrice * Quantity;

    internal void AttachTo(Order order)
    {
        if (Id > 0 && Order is not null && !ReferenceEquals(Order, order))
            throw new System.InvalidOperationException("لا يمكن نقل بند محفوظ إلى طلب آخر.");
        Order = order;
        OrderId = order.Id;
        foreach (var property in _properties) property.AttachTo(this);
    }

    internal static Result<OrderItem> Create(
        int productId,
        string productName,
        decimal unitPrice,
        int quantity,
        decimal vatRate,
        string supplierName,
        int? supplierId,
        IReadOnlyCollection<OrderItemPropertyData> properties)
    {
        if (unitPrice <= 0)
            return Result<OrderItem>.Failure("Unit price must be greater than zero.");

        if (quantity <= 0)
            return Result<OrderItem>.Failure("Quantity must be greater than zero.");

        return Result<OrderItem>.Success(
            new OrderItem(
                productId,
                productName,
                unitPrice,
                quantity,
                vatRate,
                supplierName,
                supplierId,
                properties));
    }

    public Result UpdateQuantity(int quantity)
    {
        if (Order is not null && !Order.CanEdit) return Result.Failure("بيانات الطلب مقفولة ولا تقبل التعديل.");
        if (quantity <= 0)
            return Result.Failure("Quantity must be greater than zero.");

        Quantity = quantity;

        return Result.Success();
    }

    public Result IncreaseQuantity(int quantity)
    {
        if (Order is not null && !Order.CanEdit) return Result.Failure("بيانات الطلب مقفولة ولا تقبل التعديل.");
        if (quantity > 0 && Quantity > int.MaxValue - quantity) return Result.Failure("الكمية تتجاوز الحد المسموح.");
        if (quantity <= 0)
            return Result.Failure("Quantity must be greater than zero.");

        Quantity += quantity;

        return Result.Success();
    }

    public Result DecreaseQuantity(int quantity)
    {
        if (Order is not null && !Order.CanEdit) return Result.Failure("بيانات الطلب مقفولة ولا تقبل التعديل.");
        if (quantity >= Quantity) return Result.Failure("يجب أن تظل الكمية أكبر من صفر.");
        if (quantity <= 0)
            return Result.Failure("Quantity must be greater than zero.");

        Quantity -= quantity;

        return Result.Success();
    }

    public Result ChangeUnitPrice(decimal unitPrice)
    {
        if (Order is not null && !Order.CanEdit) return Result.Failure("بيانات الطلب مقفولة ولا تقبل التعديل.");
        if (unitPrice <= 0)
            return Result.Failure("Unit price must be greater than zero.");

        UnitPrice = new Money(unitPrice);

        return Result.Success();
    }

    public void ChangeSupplier(int? supplierId, string supplierName)
    {
        if (Order is not null && !Order.CanEdit) throw new System.InvalidOperationException("بيانات الطلب مقفولة ولا تقبل التعديل.");
        SupplierId = supplierId;
        SupplierName = supplierName;
    }
}

public record OrderItemPropertyData(int PropertyId, string Value);
