using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ClientDtos;
using static OrderHub.Application.Queries.ClientQueries;

namespace OrderHub.Infrastructure.QueryHandlers.Clients;

internal class GetClientsByNameQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetClientsByNameQuery, IEnumerable<ClientListDto>>
{
    public async Task<IEnumerable<ClientListDto>> Handle(GetClientsByNameQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = appDbContextFactory.CreateDbContext();

        IQueryable<Domain.Models.Client> query = appDbContext.Clients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            string term = request.SearchTerm.Trim();
            query = query.Where(client =>
                client.Name.Value.Contains(term) ||
                client.Phone.Number.FullNumber.Contains(term) ||
                client.Address.Street.Contains(term) ||
                client.Address.City.Name.Value.Contains(term));
        }

        return await query
            .OrderBy(client => client.Name.Value)
            .Take(request.Take)
            .Select(client => new ClientListDto(
                client.Id,
                client.Name.Value,
                $"{client.Address.City.Name.Value} - {client.Address.Street}",
                client.Phone.Number.FullNumber, client.Location))
            .ToListAsync(cancellationToken);
    }
}

internal class GetClientByIdQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetClientByIdQuery, ClientListDto>
{
    public async Task<ClientListDto> Handle(GetClientByIdQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = appDbContextFactory.CreateDbContext();

        return await appDbContext.Clients
            .AsNoTracking()
            .Where(client => client.Id == request.Id)
            .Select(client => new ClientListDto(
                client.Id,
                client.Name.Value,
                $"{client.Address.City.Name.Value} - {client.Address.Street}",
                client.Phone.Number.FullNumber,
                client.Location))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
