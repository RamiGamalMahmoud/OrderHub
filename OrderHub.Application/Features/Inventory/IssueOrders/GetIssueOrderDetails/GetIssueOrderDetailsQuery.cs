using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.IssueOrders.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.IssueOrders.GetIssueOrderDetails;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Read)]
public sealed record GetIssueOrderDetailsQuery(int IssueOrderId)
    : IRequest<Result<StockIssueOrderDetails>>;
