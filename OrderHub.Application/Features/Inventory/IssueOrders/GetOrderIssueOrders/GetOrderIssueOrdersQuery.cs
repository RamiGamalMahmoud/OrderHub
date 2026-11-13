using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.IssueOrders.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Inventory.IssueOrders.GetOrderIssueOrders;

[RequirePermission(PermissionFeature.Orders, PermissionLevel.Read)]
public sealed record GetOrderIssueOrdersQuery(int OrderId)
    : IRequest<Result<IReadOnlyList<StockIssueOrderDetails>>>;
