namespace OrderHub.Domain.Models;

public class OrderItemAttribute : ModelBase
{
    private OrderItemAttribute() { }

    public OrderItemAttribute(string name, string value)
    {
        Name = name;
        Value = value;
    }

    public int OrderItemId { get; private set; }
    public OrderItem OrderItem { get; private set; }
    public string Name { get; private set; }
    public string Value { get; private set; }
}
