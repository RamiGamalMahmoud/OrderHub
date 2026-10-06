using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Orders.Update;

[RequirePermission(PermissionFeature.Orders, PermissionLevel.Write)]
public record UpdateOrderCommand(int OrderId, OrderInput.Order Order) : IRequest<Result>;
