using MediatR;
using System.Collections.Generic;
using OrderHub.Application.Features.Products.Contracts;

namespace OrderHub.Application.Features.Products;

public record GetProductsInfoQuery : IRequest<IEnumerable<ProductInfo>>;
