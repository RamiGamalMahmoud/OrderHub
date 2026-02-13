using OrderHub.Domain.Common;
using OrderHub.Domain.ValueObjects;
using System.Collections.Generic;
using System.Linq;

namespace OrderHub.Domain.Models;

public class OrderItem : ModelBase
{
    private OrderItem() { }
    private OrderItem(int productId, string productName, int orderId, decimal unitPrice, int quantity)
    {
        ProductId = productId;
        //OrderId = orderId;
        ProductName = productName;
        UnitPrice = new Money(unitPrice);
        Quantity = quantity;
    }
    public int ProductId { get; private set; }

    //public int OrderId { get; private set; }
    public string ProductName { get; private set; }

    public Money UnitPrice { get; private set; }
    public Money SubTotal => UnitPrice * Quantity;

    public int Quantity { get; private set; }

    public static Result<OrderItem> Create(int productId, string productName, int orderId, decimal unitPrice, int quantity)
    {
        var errors = new List<string>();

        if (unitPrice <= 0)
            errors.Add("Unit price must be greater than zero");

        if (quantity <= 0)
            errors.Add("Quantity must be greater than zero");

        if (errors.Any())
            return Result<OrderItem>.Failure(string.Join(", ", errors));

        return Result<OrderItem>.Success(new OrderItem(productId, productName, orderId, unitPrice, quantity));
    }

    public Result IncreaseQuantity(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure("Quantity must be greater than zero");

        Quantity += quantity;
        return Result.Success();
    }

    public Result DecreaseQuantity(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure("Quantity must be greater than zero");

        Quantity -= quantity;
        return Result.Success();
    }

    public Result UpdateQuantity(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure("Quantity must be greater than zero");

        Quantity = quantity;
        return Result.Success();
    }
}
