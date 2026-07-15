namespace OrderHub.Domain.Models;

public class OrderItemProperty : ModelBase
{
    public int OrderItemId { get; set; }

    public int PropertyId { get; set; }

    public string Value { get; set; }

    public OrderItem OrderItem { get; set; }

    public Property Property { get; set; }
}
