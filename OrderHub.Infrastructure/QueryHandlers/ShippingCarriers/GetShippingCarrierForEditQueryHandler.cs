using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ShippingCarriersDtos;
using static OrderHub.Application.Queries.ShippingCarriersQueries;

namespace OrderHub.Infrastructure.QueryHandlers.ShippingCarriers;

internal class GetShippingCarrierForEditQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetShippingCarrierForEditQuery, ShippingCarrierEditDto>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<ShippingCarrierEditDto> Handle(GetShippingCarrierForEditQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        return await appDbContext
            .ShippingCarriers
            .Where(s => s.Id == request.Id)
            .Select(s => new ShippingCarrierEditDto(
                s.Id,
                s.Name.Value,
                s.ShippingCost.Value,
                s.Phone.Number.CountryCode,
                s.Phone.Number.NationalNumber,
                s.Address.City.Id,
                s.Address.Street))
            .SingleOrDefaultAsync(cancellationToken: cancellationToken);
    }
}
