using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Clients.Contracts;

namespace OrderHub.Application.Features.Setup.Clients;

internal sealed class GetClientsInfoQueryHandler(IClientReader reader) : IRequestHandler<GetClientsInfoQuery, IEnumerable<ClientInfo>>
{
    public Task<IEnumerable<ClientInfo>> Handle(GetClientsInfoQuery request, CancellationToken cancellationToken) =>
        reader.GetClientsInfoAsync(cancellationToken);
}
