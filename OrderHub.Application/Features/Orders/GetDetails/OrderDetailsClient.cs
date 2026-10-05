using MediatR;
using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.GetDetails;

public record OrderDetailsClient(
        string Name,
        string PhoneNumber,
        string Address);
