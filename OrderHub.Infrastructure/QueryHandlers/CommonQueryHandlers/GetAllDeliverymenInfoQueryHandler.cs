using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CommonDtos;
using static OrderHub.Application.Queries.CommonQueries;

namespace OrderHub.Infrastructure.QueryHandlers.CommonQueryHandlers;

internal class GetAllDeliverymenInfoQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetAllDeliverymenInfoQuery, IEnumerable<DeliverymanInfoDto>>
{
    public async Task<IEnumerable<DeliverymanInfoDto>> Handle(GetAllDeliverymenInfoQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = appDbContextFactory.CreateDbContext();
        return await appDbContext
            .Deliverymen
            .Select(d => new DeliverymanInfoDto(d.Id, d.Name.Value))
            .ToListAsync();
    }
}
