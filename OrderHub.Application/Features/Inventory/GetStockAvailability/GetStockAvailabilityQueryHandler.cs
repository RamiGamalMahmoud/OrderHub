using MediatR;
using OrderHub.Application.Interfaces.Repositories;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.GetStockAvailability;

internal sealed class GetStockAvailabilityQueryHandler(IInventoryRepository inventory)
    : IRequestHandler<GetStockAvailabilityQuery, IReadOnlyList<StockAvailabilityRow>>
{
    public Task<IReadOnlyList<StockAvailabilityRow>> Handle(
        GetStockAvailabilityQuery request,
        CancellationToken cancellationToken) =>
        inventory.GetAvailabilityAsync(request.ProductId, cancellationToken);
}
