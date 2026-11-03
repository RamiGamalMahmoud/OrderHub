using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Application.Common.Lookups;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Setup.Clients;

[RequirePermission(PermissionFeature.Clients, PermissionLevel.Read)]
public record GetClientLookupsQuery : IRequest<IReadOnlyList<ClientLookup>>;
