using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.WhatsappGroupDtos;
using static OrderHub.Application.Queries.WhatsappGroupQueries;

namespace OrderHub.Infrastructure.QueryHandlers.WhatsappGroups;

internal class GetAllWhatsappGroupListsQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetAllWhatsappGroupListsQuery, IEnumerable<WhatsappGroupListDto>>
{
    public async Task<IEnumerable<WhatsappGroupListDto>> Handle(GetAllWhatsappGroupListsQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = appDbContextFactory.CreateDbContext();

        return await appDbContext
            .WhatsappGroups
            .Select(x => new WhatsappGroupListDto(x.Id, x.GroupName, x.GroupType))
            .ToListAsync(cancellationToken);
    }
}
