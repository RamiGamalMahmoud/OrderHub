using MediatR;
using OrderHub.Domain.Enums;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders;

public static class GetOrderItemEditor
{
    public record Query(int ProductId) : IRequest<OrderItemEditorDto>;

    public record OrderItemEditorDto(
        int ProductId,
        decimal Price,
        string Name,
        string CategoryName,
        IEnumerable<OrderItemSupplier> Suppliers,
        IEnumerable<OrderItemProperty> Properties);
    public record OrderItemSupplier(int Id, string Name);
    public record OrderItemProperty(int Id,
        string Name,
        bool IsRequired,
        PropertyType PropertyType,
        IEnumerable<OrderItemPropertyOption> Options);
    public record OrderItemPropertyOption(int Id, string Name);
    internal class Handler : IRequestHandler<Query, OrderItemEditorDto>
    {
        private readonly IOrderItemEditorReader _reader;

        public Handler(IOrderItemEditorReader reader)
        {
            _reader = reader;
        }

        public async Task<OrderItemEditorDto> Handle(Query request, CancellationToken cancellationToken)
        {
            return await _reader.GetAsync(request.ProductId, cancellationToken);
        }
    }

    public interface IOrderItemEditorReader
    {
        Task<OrderItemEditorDto> GetAsync(int productId, CancellationToken cancellationToken);
    }
}
