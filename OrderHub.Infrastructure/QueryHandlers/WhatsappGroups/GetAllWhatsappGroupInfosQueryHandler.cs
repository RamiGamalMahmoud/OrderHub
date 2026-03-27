using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.WhatsappGroupDtos;
using static OrderHub.Application.Queries.WhatsappGroupQueries;

namespace OrderHub.Infrastructure.QueryHandlers.WhatsappGroups;

internal class GetAllWhatsappGroupInfosQueryHaneler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetAllWhatsappGroupInfosQuery, IEnumerable<WhatsappGroupInfoDto>>
{
    public async Task<IEnumerable<WhatsappGroupInfoDto>> Handle(GetAllWhatsappGroupInfosQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = appDbContextFactory.CreateDbContext();

        return await appDbContext
            .WhatsappGroups
            .Where(x => x.GroupType == request.GroupType)
            .Select(x => new WhatsappGroupInfoDto(x.Id, x.GroupName, x.GroupType))
            .ToListAsync(cancellationToken);
    }
}
