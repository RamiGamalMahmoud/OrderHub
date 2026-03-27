using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.DeliverymanDtos;
using static OrderHub.Application.Queries.DeliverymanQueries;

namespace OrderHub.Infrastructure.QueryHandlers.Deliverymen;

internal class GetDeliverymenByNameQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetDeliverymenByNameQuery, IEnumerable<DeliverymanListDto>>
{
    public async Task<IEnumerable<DeliverymanListDto>> Handle(GetDeliverymenByNameQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = appDbContextFactory.CreateDbContext();

        IQueryable<Domain.Models.Deliveryman> query = appDbContext.Deliverymen.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            string term = request.SearchTerm.Trim();
            query = query.Where(deliveryman =>
                deliveryman.Name.Value.Contains(term) ||
                deliveryman.PhoneNumber.Contains(term) ||
                deliveryman.City.Name.Value.Contains(term));
        }

        return await query
            .OrderBy(deliveryman => deliveryman.Name.Value)
            .Take(request.Take)
            .Select(deliveryman => new DeliverymanListDto(
                deliveryman.Id,
                deliveryman.Name.Value,
                deliveryman.City.Name.Value,
                deliveryman.PhoneNumber))
            .ToListAsync(cancellationToken);
    }
}

internal class GetDeliverymanByIdQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetDeliverymanByIdQuery, DeliverymanListDto>
{
    public async Task<DeliverymanListDto> Handle(GetDeliverymanByIdQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = appDbContextFactory.CreateDbContext();

        return await appDbContext.Deliverymen
            .AsNoTracking()
            .Where(deliveryman => deliveryman.Id == request.Id)
            .Select(deliveryman => new DeliverymanListDto(
                deliveryman.Id,
                deliveryman.Name.Value,
                deliveryman.City.Name.Value,
                deliveryman.PhoneNumber))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
