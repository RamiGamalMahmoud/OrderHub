using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ShippingCarriersDtos;
using static OrderHub.Application.Queries.ShippingCarriersQueries;

namespace OrderHub.Infrastructure.QueryHandlers.ShippingCarriers;

internal class GetShippingCarriersQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetShippingCarriersQuery, IEnumerable<ShippingCarrierListDto>>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<IEnumerable<ShippingCarrierListDto>> Handle(GetShippingCarriersQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        return await appDbContext
            .ShippingCarriers
            .Select(s => new ShippingCarrierListDto(
                s.Id,
                s.Name.Value,
                s.ShippingCost.Value,
                $"{s.Phone.Number.FullNumber}",
                $"{s.Address.City.Name} - {s.Address.Street}"))
            .AsNoTracking()
            .ToListAsync(cancellationToken: cancellationToken);
    }
}
