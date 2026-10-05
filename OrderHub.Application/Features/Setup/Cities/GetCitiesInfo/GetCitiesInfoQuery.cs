using MediatR;
using System.Collections.Generic;
using OrderHub.Application.Features.Setup.Cities.Contracts;

namespace OrderHub.Application.Features.Setup.Cities;

public record GetCitiesInfoQuery : IRequest<IEnumerable<CityInfo>>;
