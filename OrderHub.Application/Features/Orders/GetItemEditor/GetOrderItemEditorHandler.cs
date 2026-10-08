using MediatR;
using OrderHub.Domain.Enums;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.GetItemEditor;

internal class GetOrderItemEditorHandler : IRequestHandler<GetOrderItemEditorQuery, OrderItemEditorData>
{
    private readonly IOrderItemEditorReader _reader;

    public GetOrderItemEditorHandler(IOrderItemEditorReader reader)
    {
        _reader = reader;
    }

    public async Task<OrderItemEditorData> Handle(GetOrderItemEditorQuery request, CancellationToken cancellationToken)
    {
        return await _reader.GetAsync(request.ProductId, cancellationToken);
    }
}
