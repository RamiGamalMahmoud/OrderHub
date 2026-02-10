using MediatR;

namespace OrderHub.Application.Commands;

public record AuthCommand : IRequest<bool>;
