using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.IssueOrders.RequestIssueOrderCancel;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Write)]
public sealed record RequestIssueOrderCancelCommand(int IssueOrderId, string Reason) : IRequest<Result>;
