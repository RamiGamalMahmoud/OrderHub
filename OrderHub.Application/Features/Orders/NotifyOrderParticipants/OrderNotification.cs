using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.NotifyOrderParticipants;

public sealed class OrderNotification
{
    public int Id { get; init; }
    public string OrderNumber { get; init; }
    public DateTime CreatedAt { get; init; }
    public DeliveryMethod? DeliveryMethod { get; init; }
    public decimal Total { get; init; }

    public ClientNotification Client { get; init; }

    public IReadOnlyList<OrderItemNotification> Items { get; init; }

    public DeliverymanNotification Deliveryman { get; init; }

    public ShippingCarrierNotification ShippingCarrier { get; init; }

    public IReadOnlyList<DeliveryStepNotification> DeliverySteps { get; init; }

    public IReadOnlyList<OrderEntitySequenceNotification> EntitySequences { get; init; }
}

public sealed class ClientNotification
{
    public int Id { get; init; }
    public string Name { get; init; }
    public string Phone { get; init; }
    public string Address { get; init; }
    public string Location { get; init; }
}

public sealed class OrderItemNotification
{
    public string ProductName { get; init; }
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }

    public SupplierNotification Supplier { get; init; }
}

public sealed class SupplierNotification
{
    public int Id { get; init; }
    public string Name { get; init; }
    public string WhatsappGroupLink { get; init; }
}

public sealed class DeliverymanNotification
{
    public int Id { get; init; }
    public string Name { get; init; }
    public string Phone { get; init; }
    public string City { get; init; }
    public string WhatsappGroupLink { get; init; }
}

public sealed class ShippingCarrierNotification
{
    public int Id { get; init; }
    public string Name { get; init; }
    public string Phone { get; init; }
    public string Address { get; init; }
}

public sealed class DeliveryStepNotification
{
    public int StepOrder { get; init; }
    public DeliveryMethod DeliveryMethod { get; init; }

    public DeliverymanNotification Deliveryman { get; init; }

    public ShippingCarrierNotification ShippingCarrier { get; init; }
}

public sealed class OrderEntitySequenceNotification
{
    public RecipientType RecipientType { get; init; }
    public int EntityId { get; init; }
    public string DisplayTitle { get; init; }
}