using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CityDtos;
using static OrderHub.Application.Queries.CityQueries;

namespace OrderHub.Infrastructure.QueryHandlers.Cities;

internal class GetCityForEditQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetCityForEditQuery, CityUpdateDto>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<CityUpdateDto> Handle(GetCityForEditQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        return await appDbContext
            .Cities
            .Where(city => city.Id == request.Id)
            .Select(city => new CityUpdateDto(city.Id, city.Name.Value))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
