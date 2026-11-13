using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.IssueOrders.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.IssueOrders.CreateStockIssueOrder;

[RequirePermission(PermissionFeature.Orders, PermissionLevel.Write)]
public sealed record CreateStockIssueOrderCommand(StockIssueOrderInput Input) : IRequest<Result<int>>;
