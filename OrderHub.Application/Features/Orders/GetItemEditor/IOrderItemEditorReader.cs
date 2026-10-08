using OrderHub.Application.Features.Orders.GetItemsEditor;
using MediatR;
using OrderHub.Domain.Enums;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.GetItemEditor;

public interface IOrderItemEditorReader
{
    Task<OrderItemEditorData> GetAsync(int productId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OrderCatalogItem>> GetItemsAsync(IReadOnlyCollection<int> productIds, CancellationToken cancellationToken);
}
