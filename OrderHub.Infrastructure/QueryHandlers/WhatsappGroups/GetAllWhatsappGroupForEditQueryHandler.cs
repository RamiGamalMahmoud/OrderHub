using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.WhatsappGroupDtos;
using static OrderHub.Application.Queries.WhatsappGroupQueries;

namespace OrderHub.Infrastructure.QueryHandlers.WhatsappGroups;

internal class GetAllWhatsappGroupForEditQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetAllWhatsappGroupForEditQuery, WhatsappGroupEditDto>
{
    public async Task<WhatsappGroupEditDto> Handle(GetAllWhatsappGroupForEditQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = appDbContextFactory.CreateDbContext();

        var whatsappGroup = await appDbContext
            .WhatsappGroups
            .Where(x => x.Id == request.Id)
            .Select(x => new WhatsappGroupEditDto(x.Id, x.GroupName, x.GroupType))
            .FirstOrDefaultAsync(cancellationToken);

        return whatsappGroup;
    }
}
