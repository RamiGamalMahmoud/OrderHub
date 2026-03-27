using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ClientDtos;
using static OrderHub.Application.Queries.ClientQueries;

namespace OrderHub.Infrastructure.QueryHandlers.Clients;

internal class GetClientEditQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetClientEditQuery, ClientFormDto>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<ClientFormDto> Handle(GetClientEditQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        Client client = await appDbContext
            .Clients
            .AsNoTracking()
            .Include(c => c.Address).ThenInclude(a => a.City)
            .Include(c => c.Phone)
            .Where(c => c.Id == request.Id)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        return new ClientFormDto(
            client.Name.Value, 
            client.Address.Street, 
            client.Address.City.Id, 
            client.Phone?.Number?.NationalNumber,
            client.Phone?.Number?.CountryCode);
    }
}
