using System.Collections.Generic;

namespace OrderHub.Application.Features.Products.Contracts;

public record ProductLookupItem(
    int Id,
    string Name,
    decimal Price,
    string Code,
    string CategoryName,
    IEnumerable<ProductLookupSupplier> Suppliers);

public record ProductLookupSupplier(
    int Id,
    string Name);
