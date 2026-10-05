using MediatR;
using OrderHub.Application.Common;
using System.Collections.Generic;
using OrderHub.Application.Features.Setup.Clients.Contracts;

namespace OrderHub.Application.Features.Setup.Clients;

public record GetClientEditQuery(int Id) : IRequest<ClientInput>;
