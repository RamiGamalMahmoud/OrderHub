using MediatR;
using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.GetDetails;

public record OrderDetailsLine(
        int ProductId,
        string ProductName,
        decimal UnitPrice,
        int Quantity,
        decimal Total);
