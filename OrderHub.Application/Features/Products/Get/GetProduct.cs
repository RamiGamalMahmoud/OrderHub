using MediatR;
using OrderHub.Application.Features.Products.Contracts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Products.Get;

public static class GetProduct
{
    public record Query(int Id) : IRequest<ProductDetails>;

    public record ProductDetails(
        string Name,
        string Code,
        decimal Price,
        int CategoryId,
        IEnumerable<int> SelectedSuppliersIds,
        IEnumerable<ProductProperty> ProductProperties);

    public record ProductProperty();

    internal class Handler : IRequestHandler<Query, ProductDetails>
    {
        private readonly IProductStore _productStore;

        public Handler(IProductStore productStore)
        {
            _productStore = productStore;
        }

        public async Task<ProductDetails> Handle(Query request, CancellationToken cancellationToken)
        {
            return await _productStore.GetProductAsync(request.Id, cancellationToken);
        }
    }
}
