using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Counts.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.Counts.ApproveCount;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Approve)]
public sealed record ApproveCountCommand(
    int CountId,
    string Note,
    int ExpectedVersion) : IRequest<Result<CountDecisionResult>>;
