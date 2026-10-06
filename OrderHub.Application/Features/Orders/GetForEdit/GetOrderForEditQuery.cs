using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.GetForEdit;

[RequirePermission(PermissionFeature.Orders, PermissionLevel.Read)]
public record GetOrderForEditQuery(int OrderId) : IRequest<OrderEditorData>;
