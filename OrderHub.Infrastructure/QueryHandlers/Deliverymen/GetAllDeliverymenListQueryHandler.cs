using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.DeliverymanDtos;
using static OrderHub.Application.Queries.DeliverymanQueries;

namespace OrderHub.Infrastructure.QueryHandlers.Deliverymen;

internal class GetAllDeliverymenListQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetAllDeliverymenListQuery, IEnumerable<DeliverymanListDto>>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<IEnumerable<DeliverymanListDto>> Handle(GetAllDeliverymenListQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
        return await appDbContext.Deliverymen
            .Select(d => new DeliverymanListDto(d.Id, d.Name.Value, d.City.Name.Value, d.PhoneNumber))
            .AsNoTracking()
            .ToListAsync();
    }
}
