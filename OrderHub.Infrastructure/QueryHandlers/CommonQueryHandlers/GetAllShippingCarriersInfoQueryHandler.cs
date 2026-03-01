using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CommonDtos;
using static OrderHub.Application.Queries.CommonQueries;

namespace OrderHub.Infrastructure.QueryHandlers.CommonQueryHandlers;

internal class GetAllShippingCarriersInfoQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetAllShippingCarriersInfoQuery, IEnumerable<ShippingCarrierInfoDto>>
{
    public async Task<IEnumerable<ShippingCarrierInfoDto>> Handle(GetAllShippingCarriersInfoQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = appDbContextFactory.CreateDbContext();
        return await appDbContext.ShippingCarriers
            .Select(s => new ShippingCarrierInfoDto(s.Id, s.Name.Value))
            .ToListAsync();
    }
}
