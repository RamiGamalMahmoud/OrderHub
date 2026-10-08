using MediatR;
using OrderHub.Application.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Clients.Contracts;
using OrderHub.Application.Features.Setup.Clients;

namespace OrderHub.Application.Features.Setup.Clients;

internal sealed class GetAllClientsPagedQueryHandler(IClientReader reader) : IRequestHandler<GetAllClientsPagedQuery, PagedResult<ClientListItem>>
{
    public Task<PagedResult<ClientListItem>> Handle(GetAllClientsPagedQuery request, CancellationToken cancellationToken) =>
        reader.GetAllClientsPagedAsync(request.PageNumber, request.PageSize, cancellationToken);
}
