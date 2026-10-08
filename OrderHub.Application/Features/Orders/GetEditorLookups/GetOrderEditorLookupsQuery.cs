using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;

namespace OrderHub.Application.Features.Orders.GetEditorLookups;

[RequirePermission(PermissionFeature.Orders, PermissionLevel.Read)]
public record GetOrderEditorLookupsQuery : IRequest<OrderEditorLookups>;
