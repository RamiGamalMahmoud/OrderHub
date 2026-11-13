using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Returns.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.Returns.GetReturnsOverview;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Read)]
public sealed record GetReturnsOverviewQuery : IRequest<Result<ReturnsOverview>>;
