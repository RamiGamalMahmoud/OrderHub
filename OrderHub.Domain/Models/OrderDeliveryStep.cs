using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Domain.Models;

public class OrderDeliveryStep : ModelBase
{
    private readonly List<Shipment> _shipments = [];
    private readonly List<DeliveryTrip> _deliveryTrips = [];
    public DeliveryStatus Status { get; private set; } = DeliveryStatus.Pending;
    public IReadOnlyCollection<Shipment> Shipments => _shipments.AsReadOnly();
    public IReadOnlyCollection<DeliveryTrip> DeliveryTrips => _deliveryTrips.AsReadOnly();

    internal Shipment StartShipment() { var item = new Shipment(null, this); _shipments.Add(item); return item; }
    internal DeliveryTrip StartDeliveryTrip() { var item = new DeliveryTrip(null, this); _deliveryTrips.Add(item); return item; }

    internal void RecordStatus(DeliveryStatus status) => Status = status;

    public int OrderId { get; private set; }
    public Order Order { get; private set; }

    private int _stepOrder;
    private DeliveryMethod _deliveryMethod;
    private int? _deliverymanId, _shippingCarrierId;
    public int StepOrder { get => _stepOrder; set { EnsurePlanning(); _stepOrder = value; } }
    public DeliveryMethod DeliveryMethod { get => _deliveryMethod; set { EnsurePlanning(); _deliveryMethod = value; } }

    public int? DeliverymanId { get => _deliverymanId; set { EnsurePlanning(); _deliverymanId = value; } }
    public Deliveryman Deliveryman { get; private set; }

    public int? ShippingCarrierId { get => _shippingCarrierId; set { EnsurePlanning(); _shippingCarrierId = value; } }
    public ShippingCarrier ShippingCarrier { get; private set; }

    internal void AttachTo(Order order)
    {
        if (Id > 0 && Order is not null && !ReferenceEquals(Order, order))
            throw new System.InvalidOperationException("لا يمكن نقل خطوة محفوظة إلى طلب آخر.");
        Order = order;
        OrderId = order.Id;
    }

    private void EnsurePlanning()
    {
        if (Order is not null && !Order.CanEdit)
            throw new System.InvalidOperationException("خطة التوصيل مقفولة ولا تقبل التعديل.");
    }
}
