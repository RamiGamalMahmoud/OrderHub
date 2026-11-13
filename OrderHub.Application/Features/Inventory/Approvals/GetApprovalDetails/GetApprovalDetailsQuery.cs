using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Approvals.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.Approvals.GetApprovalDetails;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Approve)]
public sealed record GetApprovalDetailsQuery(int ApprovalId) : IRequest<Result<ApprovalDetailsResult>>;
