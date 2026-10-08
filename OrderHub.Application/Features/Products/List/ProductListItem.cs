using MediatR;
using OrderHub.Application.Features.Products.Contracts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Products.List;

public record ProductListItem(
        int Id,
        string Name,
        decimal Price,
        string Code,
        string CategoryName,
        IReadOnlyCollection<ProductSupplierItem> Suppliers);
