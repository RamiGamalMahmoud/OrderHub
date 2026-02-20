using MediatR;
using System.Collections.Generic;
using static OrderHub.Application.DTOs.ProductDtos;

namespace OrderHub.Application.Queries;

public static class ProductQueries
{
    public record GetAllProductsQuery() : IRequest<IEnumerable<ProductListDto>>;
    public record GetProductsByNameQuery(string SearchTerm) : IRequest<IEnumerable<ProductListDto>>;
    public record GetProductForEditQuery(int Id) : IRequest<ProductEdtiDto>;
    public record GetProductsByCategoryQuery(int CategoryId) : IRequest<IEnumerable<ProductListDto>>;
}
