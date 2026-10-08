using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Setup.Properties.Update;

[RequirePermission(PermissionFeature.Properties, PermissionLevel.Write)]
public sealed record UpdatePropertyCommand(PropertyUpdateInput Input) : IRequest;
