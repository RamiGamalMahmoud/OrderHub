using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Domain.Common;
using System;

namespace OrderHub.Application.Features.Orders.Create;

[RequirePermission(PermissionFeature.Orders, PermissionLevel.Write)]
public record CreateOrderCommand(Guid? OrderDraftId, OrderInput.Order Order, Guid? CreationRequestId = null) : IRequest<Result<int>>;
