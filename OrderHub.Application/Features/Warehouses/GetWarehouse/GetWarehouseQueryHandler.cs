using MediatR;
using OrderHub.Application.Features.Warehouses.Contracts;
using OrderHub.Application.Interfaces.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Warehouses;

internal sealed class GetWarehouseQueryHandler(IWarehouseRepository repository)
    : IRequestHandler<GetWarehouseQuery, WarehouseEdit>
{
    public Task<WarehouseEdit> Handle(
        GetWarehouseQuery request,
        CancellationToken cancellationToken) =>
        repository.GetForEditAsync(request.Id, cancellationToken);
}
