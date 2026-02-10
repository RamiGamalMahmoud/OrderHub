using OrderHub.Domain.Common;
using OrderHub.Domain.ValueObjects;
using System.Collections.Generic;
using System.Linq;

namespace OrderHub.Domain.Models;

public class OrderItem : ModelBase
{
    private OrderItem() { }
    private OrderItem(Product product, int quantity)
    {
        Product = product;
        UnitPrice = product.Price;
        Quantity = quantity;
    }

    public Product Product { get; private set; }
    public Order Order { get; private set; }
    public Money UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public Money TotalPrice => UnitPrice * Quantity;

    public static Result<OrderItem> Create(Product product, int quantity)
    {
        var errors = new List<string>();

        if (product is null)
            errors.Add("Product is required");

        if (quantity <= 0)
            errors.Add("Quantity must be greater than zero");

        if (errors.Any())
            return Result<OrderItem>.Failure(string.Join(", ", errors));

        return Result<OrderItem>.Success(new OrderItem(product, quantity));
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
