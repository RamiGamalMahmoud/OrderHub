using MediatR;
using OrderHub.Application.Features.Products.Contracts;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Products.Search;

public record SearchProductsQuery(string SearchTerm) : IRequest<IEnumerable<ProductLookupItem>>;
