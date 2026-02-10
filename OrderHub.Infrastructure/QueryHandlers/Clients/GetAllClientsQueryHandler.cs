using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ClientDtos;
using static OrderHub.Application.Queries.ClientQueries;

namespace OrderHub.Infrastructure.QueryHandlers.Clients;

internal class GetAllClientsQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetAllClientsQuery, IEnumerable<ClientListDto>>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<IEnumerable<ClientListDto>> Handle(GetAllClientsQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        return await appDbContext
            .Clients
            .AsNoTracking()
            .Select(c => new ClientListDto(
                c.Id, 
                c.Name.Value, 
                $"{c.Address.City.Name.Value} - {c.Address.Street}",
                c.Phone.Number.FullNumber)
            )
            .ToListAsync(cancellationToken: cancellationToken);
    }
}
