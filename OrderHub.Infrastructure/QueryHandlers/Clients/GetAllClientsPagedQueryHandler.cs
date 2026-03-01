using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common;
using OrderHub.Infrastructure.Extensions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ClientDtos;
using static OrderHub.Application.Queries.ClientQueries;

namespace OrderHub.Infrastructure.QueryHandlers.Clients;

internal class GetAllClientsPagedQueryHandler(AppDbContextFactory appDbContextFactory) 
    : IRequestHandler<GetAllClientsPagedQuery, PagedResult<ClientListDto>>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<PagedResult<ClientListDto>> Handle(GetAllClientsPagedQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        var query = appDbContext.Clients
            .AsNoTracking()
            .OrderBy(c => c.Name.Value)
            .Select(c => new ClientListDto(
                c.Id,
                c.Name.Value,
                $"{c.Address.City.Name.Value} - {c.Address.Street}",
                c.Phone.Number.FullNumber));

        return await query.ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);
    }
}
