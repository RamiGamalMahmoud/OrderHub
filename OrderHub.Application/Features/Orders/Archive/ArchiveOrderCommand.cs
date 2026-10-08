using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Orders.Lifecycle;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Orders.Archive;

[RequirePermission(PermissionFeature.Orders, PermissionLevel.Write)]
public sealed record ArchiveOrderCommand(int OrderId, int ExpectedVersion, string Reason)
    : IRequest<Result<OrderOperationReceipt>>;
