using MediatR;
using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.GetDetails;

public record OrderDetailsDelivery(
        string DeliveryMethod,
        string DeliverymanName,
        string ShippingCarrierName);
