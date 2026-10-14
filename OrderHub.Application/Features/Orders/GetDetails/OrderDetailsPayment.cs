using MediatR;
using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.GetDetails;

public record OrderDetailsPayment(
        string PaymentMethod,
        decimal ProductsTotal,
        decimal VatAmount,
        decimal ShippingCost,
        decimal Discount,
        decimal GrandTotal);
