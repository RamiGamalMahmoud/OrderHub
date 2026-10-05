using OrderHub.Application.Features.Orders.GetItemEditor;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.GetItemsEditor;

internal sealed class GetOrderItemsEditorHandler(IOrderItemEditorReader reader) : IRequestHandler<GetOrderItemsEditorQuery, IReadOnlyList<OrderCatalogItem>>
{
    public Task<IReadOnlyList<OrderCatalogItem>> Handle(GetOrderItemsEditorQuery request, CancellationToken cancellationToken) =>
        reader.GetItemsAsync(request.ProductIds, cancellationToken);
}
