using MediatR;
using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.GetDetails;

public record GetOrderDetailsQuery(int Id) : IRequest<OrderDetails>;
