using MediatR;
using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.GetDetails;

public record OrderDetails(
        OrderDetailsHeader Header,
        OrderDetailsClient Client,
        OrderDetailsDelivery Delivery,
        IEnumerable<OrderDetailsLine> Items,
        OrderDetailsPayment Payment);
