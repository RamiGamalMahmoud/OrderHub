using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Counts.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.Counts.GetCounts;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Read)]
public sealed record GetCountsQuery : IRequest<Result<CountOverview>>;
