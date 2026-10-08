using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Clients.Contracts;
using OrderHub.Application.Features.Setup.Clients;

namespace OrderHub.Application.Features.Setup.Clients;

internal sealed class GetClientByIdQueryHandler(IClientReader reader) : IRequestHandler<GetClientByIdQuery, ClientListItem>
{
    public Task<ClientListItem> Handle(GetClientByIdQuery request, CancellationToken cancellationToken) =>
        reader.GetClientByIdAsync(request.Id, cancellationToken);
}
