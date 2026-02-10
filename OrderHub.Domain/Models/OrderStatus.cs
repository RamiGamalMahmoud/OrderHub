namespace OrderHub.Domain.Models;

public class OrderStatus : ModelBase
{
    public string Status { get; private set; }
    public string DisplayName { get; private set; }
    private OrderStatus() { }
    public OrderStatus(string status, string displayName) => (Status, DisplayName) = (status, displayName);
}
