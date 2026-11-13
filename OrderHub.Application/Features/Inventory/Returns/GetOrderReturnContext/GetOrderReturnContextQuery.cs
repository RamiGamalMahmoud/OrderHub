using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Returns.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.Returns.GetOrderReturnContext;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Write)]
public sealed record GetOrderReturnContextQuery(int OrderId) : IRequest<Result<OrderReturnContext>>;
