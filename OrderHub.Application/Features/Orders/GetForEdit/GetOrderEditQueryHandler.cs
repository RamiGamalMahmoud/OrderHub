using MediatR;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Orders.Contracts;

namespace OrderHub.Application.Features.Orders.GetForEdit;

internal sealed class GetOrderEditQueryHandler(IOrderReader reader) : IRequestHandler<GetOrderForEditQuery, OrderEditorData>
{
    public Task<OrderEditorData> Handle(GetOrderForEditQuery request, CancellationToken cancellationToken) =>
        reader.GetForEditAsync(request.OrderId, cancellationToken);
}
