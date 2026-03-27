using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ShippingCarriersDtos;
using static OrderHub.Application.Queries.ShippingCarriersQueries;

namespace OrderHub.Infrastructure.QueryHandlers.ShippingCarriers;

internal class GetShippingCarriersByNameQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetShippingCarriersByNameQuery, IEnumerable<ShippingCarrierListDto>>
{
    public async Task<IEnumerable<ShippingCarrierListDto>> Handle(GetShippingCarriersByNameQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = appDbContextFactory.CreateDbContext();

        IQueryable<Domain.Models.ShippingCarrier> query = appDbContext.ShippingCarriers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            string term = request.SearchTerm.Trim();
            query = query.Where(carrier =>
                carrier.Name.Value.Contains(term) ||
                carrier.Phone.Number.FullNumber.Contains(term) ||
                carrier.Address.Street.Contains(term) ||
                carrier.Address.City.Name.Value.Contains(term));
        }

        return await query
            .OrderBy(carrier => carrier.Name.Value)
            .Take(request.Take)
            .Select(carrier => new ShippingCarrierListDto(
                carrier.Id,
                carrier.Name.Value,
                carrier.ShippingCost.Value,
                carrier.Phone.Number.FullNumber,
                $"{carrier.Address.City.Name.Value} - {carrier.Address.Street}"))
            .ToListAsync(cancellationToken);
    }
}

internal class GetShippingCarrierByIdQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetShippingCarrierByIdQuery, ShippingCarrierListDto>
{
    public async Task<ShippingCarrierListDto> Handle(GetShippingCarrierByIdQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = appDbContextFactory.CreateDbContext();

        return await appDbContext.ShippingCarriers
            .AsNoTracking()
            .Where(carrier => carrier.Id == request.Id)
            .Select(carrier => new ShippingCarrierListDto(
                carrier.Id,
                carrier.Name.Value,
                carrier.ShippingCost.Value,
                carrier.Phone.Number.FullNumber,
                $"{carrier.Address.City.Name.Value} - {carrier.Address.Street}"))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
