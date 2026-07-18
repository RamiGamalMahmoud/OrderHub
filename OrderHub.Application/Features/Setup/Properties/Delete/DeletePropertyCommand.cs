using MediatR;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Setup.Properties.Delete;

public sealed record DeletePropertyCommand(int Id) : IRequest<Result>;
