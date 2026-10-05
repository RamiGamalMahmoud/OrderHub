using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Application.Features.Setup.Cities.Contracts;

namespace OrderHub.Application.Features.Setup.Cities;

public record DeleteCityCommand(int Id) : IRequest<Result>;
