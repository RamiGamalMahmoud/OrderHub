using OrderHub.Domain.Enums;

namespace OrderHub.Domain.Models;

public class OrderDeliveryStep : ModelBase
{
    public int OrderId { get; set; }
    public Order Order { get; set; }

    public int StepOrder { get; set; }
    public DeliveryMethod DeliveryMethod { get; set; }

    public int? DeliverymanId { get; set; }
    public Deliveryman Deliveryman { get; set; }

    public int? ShippingCarrierId { get; set; }
    public ShippingCarrier ShippingCarrier { get; set; }
}
