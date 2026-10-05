using MediatR;
using OrderHub.Application.Common;
using System.Collections.Generic;
using OrderHub.Application.Features.Setup.Clients.Contracts;

namespace OrderHub.Application.Features.Setup.Clients;

public record GetAllClientsQuery : IRequest<IEnumerable<ClientListItem>>;
