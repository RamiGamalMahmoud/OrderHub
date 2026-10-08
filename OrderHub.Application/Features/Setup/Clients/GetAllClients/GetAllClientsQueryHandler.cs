using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Clients.Contracts;
using OrderHub.Application.Features.Setup.Clients;

namespace OrderHub.Application.Features.Setup.Clients;

internal sealed class GetAllClientsQueryHandler(IClientReader reader) : IRequestHandler<GetAllClientsQuery, IEnumerable<ClientListItem>>
{
    public Task<IEnumerable<ClientListItem>> Handle(GetAllClientsQuery request, CancellationToken cancellationToken) =>
        reader.GetAllClientsAsync(cancellationToken);
}
