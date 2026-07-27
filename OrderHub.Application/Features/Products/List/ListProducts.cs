using MediatR;
using OrderHub.Application.Features.Products.Contracts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Products.List;

public static class ListProducts
{
    public record Query : IRequest<IEnumerable<ProductDto>>;

    public record ProductDto(
        int Id,
        string Name,
        decimal Price,
        string Code,
        string CategoryName,
        IReadOnlyCollection<ProductSupplier> Suppliers);

    public record ProductSupplier(
        int Id,
        string Name);

    internal class Handler : IRequestHandler<Query, IEnumerable<ProductDto>>
    {
        private readonly IProductStore _productStore;

        public Handler(IProductStore productStore)
        {
            _productStore = productStore;
        }

        public async Task<IEnumerable<ProductDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            return await _productStore.GetListProductsAsync();
        }
    }
}
