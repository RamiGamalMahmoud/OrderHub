using MediatR;
using System.Collections.Generic;
using OrderHub.Application.Features.Setup.Clients.Contracts;

namespace OrderHub.Application.Features.Setup.Clients;

public record GetClientsInfoQuery : IRequest<IEnumerable<ClientInfo>>;
