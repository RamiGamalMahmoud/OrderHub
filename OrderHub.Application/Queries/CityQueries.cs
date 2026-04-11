using MediatR;
using System.Collections.Generic;
using static OrderHub.Application.DTOs.CityDtos;

namespace OrderHub.Application.Queries;

public static class CityQueries
{
    public record GetAllCitiesQuery : IRequest<IEnumerable<CityListDto>>;
    public record GetCityForEditQuery(int Id) : IRequest<CityUpdateDto>;
}
