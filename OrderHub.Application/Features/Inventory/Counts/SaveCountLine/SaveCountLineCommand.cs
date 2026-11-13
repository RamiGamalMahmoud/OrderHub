using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.Counts.SaveCountLine;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Write)]
public sealed record SaveCountLineCommand(
    int CountId,
    int LineId,
    int? CountedQty,
    int? CountedDamagedQty) : IRequest<Result>;
