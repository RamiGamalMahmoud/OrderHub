using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Application.Features.Setup.Clients.Contracts;

namespace OrderHub.Application.Features.Setup.Clients;

public record CreateClientCommand(ClientInput Client) : IRequest<Result>;
