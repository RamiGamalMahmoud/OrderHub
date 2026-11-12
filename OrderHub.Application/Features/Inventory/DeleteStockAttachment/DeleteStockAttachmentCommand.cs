using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.DeleteStockAttachment;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Write)]
public sealed record DeleteStockAttachmentCommand(int AttachmentId) : IRequest<Result>;
