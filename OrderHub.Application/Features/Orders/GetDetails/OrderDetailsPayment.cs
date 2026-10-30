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
        decimal GrandTotal,
        decimal ProductsTotalIncludingVat = 0,
        OrderHub.Domain.Enums.DiscountType DiscountType = OrderHub.Domain.Enums.DiscountType.FixedAmount,
        decimal DiscountValue = 0);
