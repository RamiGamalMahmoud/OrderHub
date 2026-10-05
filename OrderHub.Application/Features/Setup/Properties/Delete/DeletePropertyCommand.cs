using MediatR;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Setup.Properties.Delete;

public record DeletePropertyCommand(int Id) : IRequest<Result>;
