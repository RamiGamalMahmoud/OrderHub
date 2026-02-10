using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CommonDtos;
using static OrderHub.Application.Queries.CommonQueries;

namespace OrderHub.Infrastructure.QueryHandlers.CommonQueryHandlers;

internal class GetClientsInfoQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetClientsInfoQuery, IEnumerable<ClientInfoDto>>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<IEnumerable<ClientInfoDto>> Handle(GetClientsInfoQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
        return await appDbContext
            .Clients
            .Select(c => new ClientInfoDto(c.Id, c.Name.Value))
            .ToListAsync(cancellationToken: cancellationToken);
    }
}
