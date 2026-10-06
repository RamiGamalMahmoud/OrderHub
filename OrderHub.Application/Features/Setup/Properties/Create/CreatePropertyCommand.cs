using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Setup.Properties.Create;

[RequirePermission(PermissionFeature.Properties, PermissionLevel.Write)]
public sealed record CreatePropertyCommand(
    string Name,
    PropertyType PropertyType,
    string Description,
    IReadOnlyCollection<PropertyOptionInput> Options) : IRequest<int>;
