using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CommonDtos;
using static OrderHub.Application.Queries.CommonQueries;

namespace OrderHub.Infrastructure.QueryHandlers.CommonQueryHandlers;

internal class GetOrderStautsesInfoHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetOrderStautsesInfoQuery, IEnumerable<OrderStatusInfoDto>>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<IEnumerable<OrderStatusInfoDto>> Handle(GetOrderStautsesInfoQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
        return await appDbContext.OrderStatuses
            .Select(o => new OrderStatusInfoDto(o.Id, o.Status, o.DisplayName))
            .ToListAsync(cancellationToken: cancellationToken);
    }
}
