using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Setup.Properties.Delete;

[RequirePermission(PermissionFeature.Properties, PermissionLevel.Write)]
public record DeletePropertyCommand(int Id) : IRequest<Result>;
