using OrderHub.Domain.Common;
using OrderHub.Domain.ValueObjects;

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
        string supplierName,
        int? supplierId)
    {
        ProductId = productId;
        ProductName = productName;
        UnitPrice = new Money(unitPrice);
        Quantity = quantity;
        SupplierName = supplierName;
        SupplierId = supplierId;
    }

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

    public Money SubTotal => UnitPrice * Quantity;

    internal static Result<OrderItem> Create(
        int productId,
        string productName,
        decimal unitPrice,
        int quantity,
        string supplierName,
        int? supplierId)
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
                supplierName,
                supplierId));
    }

    public Result UpdateQuantity(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure("Quantity must be greater than zero.");

        Quantity = quantity;

        return Result.Success();
    }

    public Result IncreaseQuantity(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure("Quantity must be greater than zero.");

        Quantity += quantity;

        return Result.Success();
    }

    public Result DecreaseQuantity(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure("Quantity must be greater than zero.");

        Quantity -= quantity;

        return Result.Success();
    }

    public Result ChangeUnitPrice(decimal unitPrice)
    {
        if (unitPrice <= 0)
            return Result.Failure("Unit price must be greater than zero.");

        UnitPrice = new Money(unitPrice);

        return Result.Success();
    }

    public void ChangeSupplier(int? supplierId, string supplierName)
    {
        SupplierId = supplierId;
        SupplierName = supplierName;
    }
}