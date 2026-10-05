using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Application.Features.Setup.Clients.Contracts;

namespace OrderHub.Application.Features.Setup.Clients;

public record UpdateClientCommand(int Id, ClientInput Client) : IRequest<Result>;
