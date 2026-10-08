using MediatR;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Domain.Common;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Products.Update;

public record ProductUpdateInput(
        string Name,
        string Code,
        decimal Price,
        int CategoryId,
        IEnumerable<int> SelectedSuppliersIds,
        IEnumerable<ProductPropertyUpdate> ProductProperties);
