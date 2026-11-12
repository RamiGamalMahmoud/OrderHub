using MediatR;
using OrderHub.Application.Features.Warehouses.Contracts;
using OrderHub.Application.Interfaces.Repositories;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Warehouses;

internal sealed class GetWarehousesQueryHandler(IWarehouseRepository repository)
    : IRequestHandler<GetWarehousesQuery, IReadOnlyList<WarehouseListItem>>
{
    public Task<IReadOnlyList<WarehouseListItem>> Handle(
        GetWarehousesQuery request,
        CancellationToken cancellationToken) =>
        repository.GetListAsync(cancellationToken);
}
