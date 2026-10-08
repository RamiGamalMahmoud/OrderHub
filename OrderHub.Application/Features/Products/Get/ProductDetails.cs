using MediatR;
using OrderHub.Application.Features.Products.Contracts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Products.Get;

public record ProductDetails(
        string Name,
        string Code,
        decimal Price,
        int CategoryId,
        IEnumerable<int> SelectedSuppliersIds,
        IEnumerable<ProductPropertySelection> ProductProperties);
