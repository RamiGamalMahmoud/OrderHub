using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.DeliverymanDtos;
using static OrderHub.Application.Queries.DeliverymanQueries;

namespace OrderHub.Infrastructure.QueryHandlers.Deliverymen;

internal class GetDeliverymanForEditQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetDeliverymanForEditQuery, DeliverymanFormDto>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<DeliverymanFormDto> Handle(GetDeliverymanForEditQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        return await appDbContext.Deliverymen
            .Where(d => d.Id == request.Id)
            .Select(d => new DeliverymanFormDto(d.Name.Value, d.City.Id, d.PhoneNumber))
            .FirstOrDefaultAsync();
    }
}
