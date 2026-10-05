using MediatR;
using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.GetDetails;

public record OrderDetailsHeader(
        int Id,
        string OrderNumber,
        DateTime CreatedAt,
        string OrderStatus);
