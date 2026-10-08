using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.GetDetails;

[RequirePermission(PermissionFeature.Orders, PermissionLevel.Read)]
public record GetOrderDetailsQuery(int Id) : IRequest<OrderDetails>;
