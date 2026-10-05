using MediatR;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Clients.Contracts;
using OrderHub.Application.Features.Setup.Clients;

namespace OrderHub.Application.Features.Setup.Clients;

internal sealed class GetClientEditQueryHandler(IClientReader reader) : IRequestHandler<GetClientEditQuery, ClientInput>
{
    public Task<ClientInput> Handle(GetClientEditQuery request, CancellationToken cancellationToken) =>
        reader.GetClientEditAsync(request.Id, cancellationToken);
}
