using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using System.Collections.Generic;
using OrderHub.Application.Features.Setup.Clients.Contracts;

namespace OrderHub.Application.Features.Setup.Clients;

[RequirePermission(PermissionFeature.Clients, PermissionLevel.Read)]
public record GetClientsInfoQuery : IRequest<IEnumerable<ClientInfo>>;
