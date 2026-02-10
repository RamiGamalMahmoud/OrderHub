using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CommonDtos;
using static OrderHub.Application.Queries.CommonQueries;

namespace OrderHub.Infrastructure.QueryHandlers.CommonQueryHandlers;

internal class GetCitiesInfoQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetCitiesInfoQuery, IEnumerable<CityInfoDto>>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<IEnumerable<CityInfoDto>> Handle(GetCitiesInfoQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        return await appDbContext
            .Cities
            .Select(c => new CityInfoDto(c.Id, c.Name.Value))
            .ToListAsync(cancellationToken: cancellationToken);
    }
}
