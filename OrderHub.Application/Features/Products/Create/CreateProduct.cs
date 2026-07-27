using MediatR;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Domain.Common;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Products.Create;

public static class CreateProduct
{
    public record Command(ProductDto Product) : IRequest<Result<int>>;

    public record ProductDto(
            string Name,
            string Code,
            decimal Price,
            int CategoryId,
            IEnumerable<int> SelectedSuppliersIds,
            IEnumerable<ProductPropertiesDto> ProductProperties);

    public record ProductPropertiesDto(
            int PropertyId,
            bool IsRequired);

    internal class Handler : IRequestHandler<Command, Result<int>>
    {
        private readonly IProductStore _productStore;

        public Handler(IProductStore productStore)
        {
            _productStore = productStore;
        }

        public async Task<Result<int>> Handle(Command request, CancellationToken cancellationToken)
        {
            return await _productStore.CreateAsync(request.Product);
        }
    }
}
