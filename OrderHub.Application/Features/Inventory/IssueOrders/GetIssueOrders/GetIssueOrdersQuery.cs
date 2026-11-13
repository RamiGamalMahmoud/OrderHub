using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.IssueOrders.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.IssueOrders.GetIssueOrders;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Read)]
public sealed record GetIssueOrdersQuery : IRequest<Result<StockIssueOrderList>>;
