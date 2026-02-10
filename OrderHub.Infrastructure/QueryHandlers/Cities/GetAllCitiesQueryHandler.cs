using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CityDtos;
using static OrderHub.Application.Queries.CityQueries;

namespace OrderHub.Infrastructure.QueryHandlers.Cities;

internal class GetAllCitiesQueryHandler : IRequestHandler<GetAllCitiesQuery, IEnumerable<CityListDto>>
{
    private readonly AppDbContextFactory _appDbContextFactory;

    public GetAllCitiesQueryHandler(AppDbContextFactory appDbContextFactory)
    {
        _appDbContextFactory = appDbContextFactory;
    }

    public async Task<IEnumerable<CityListDto>> Handle(GetAllCitiesQuery request, CancellationToken cancellationToken)
    {
        using (AppDbContext appDbContext = _appDbContextFactory.CreateDbContext())
        {
            return await appDbContext
                .Cities
                .Select(c => new CityListDto(c.Id, c.Name.Value))
                .ToListAsync(cancellationToken: cancellationToken);
        }
    }
}
