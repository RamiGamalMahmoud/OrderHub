using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Domain.Enums;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.GetItemEditor;

[RequirePermission(PermissionFeature.Orders, PermissionLevel.Read)]
public record GetOrderItemEditorQuery(int ProductId) : IRequest<OrderItemEditorData>;
