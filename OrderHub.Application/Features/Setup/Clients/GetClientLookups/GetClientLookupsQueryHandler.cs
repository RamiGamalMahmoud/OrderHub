using MediatR;
using OrderHub.Application.Common.Lookups;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Clients.Contracts;

namespace OrderHub.Application.Features.Setup.Clients;

internal sealed class GetClientLookupsQueryHandler(IClientReader reader) : IRequestHandler<GetClientLookupsQuery, IReadOnlyList<ClientLookup>>
{
    public Task<IReadOnlyList<ClientLookup>> Handle(GetClientLookupsQuery request, CancellationToken cancellationToken) =>
        reader.GetClientLookupsAsync(cancellationToken);
}
