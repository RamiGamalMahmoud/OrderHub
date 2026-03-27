using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Queries.WhatsappGroupQueries;

namespace OrderHub.Infrastructure.QueryHandlers.WhatsappGroups;

internal class IsWhatsappGroupExistsQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<IsWhatsappGroupExistsQuery, bool>
{
    public async Task<bool> Handle(IsWhatsappGroupExistsQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = appDbContextFactory.CreateDbContext();

        return await appDbContext
            .WhatsappGroups
            .AnyAsync(x => x.GroupName == request.GroupName, cancellationToken);
    }
}
