using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Application.Common;
using System.Collections.Generic;
using OrderHub.Application.Features.Setup.Clients.Contracts;

namespace OrderHub.Application.Features.Setup.Clients;

[RequirePermission(PermissionFeature.Clients, PermissionLevel.Read)]
public record GetClientsByNameQuery(string SearchTerm = null, int Take = 20) : IRequest<IEnumerable<ClientListItem>>;
