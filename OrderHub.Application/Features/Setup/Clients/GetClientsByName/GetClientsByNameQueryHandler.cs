using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Clients.Contracts;
using OrderHub.Application.Features.Setup.Clients;

namespace OrderHub.Application.Features.Setup.Clients;

internal sealed class GetClientsByNameQueryHandler(IClientReader reader) : IRequestHandler<GetClientsByNameQuery, IEnumerable<ClientListItem>>
{
    public Task<IEnumerable<ClientListItem>> Handle(GetClientsByNameQuery request, CancellationToken cancellationToken) =>
        reader.GetClientsByNameAsync(request.SearchTerm, request.Take, cancellationToken);
}
