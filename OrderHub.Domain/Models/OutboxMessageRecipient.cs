namespace OrderHub.Domain.Models;

public abstract class OutboxMessageRecipient : ModelBase
{
    public string Name { get; set; }

    public string PhoneNumber { get; set; }
}

public class ClientRecipient : OutboxMessageRecipient
{
    public int ClientId { get; set; }
    public Client Client { get; set; }
}

public class SupplierRecipient : OutboxMessageRecipient
{
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; }
}

public class DeliverymanRecipient : OutboxMessageRecipient
{
    public int DeliveryManId { get; set; }
    public Deliveryman DeliveryMan { get; set; }
}

public class ShippingCarrierRecipient : OutboxMessageRecipient
{
    public int ShippingCarrierId { get; set; }
    public ShippingCarrier ShippingCarrier { get; set; }
}
