//using MediatR;
//using OrderHub.Application.Common;
//using System.Collections.Generic;
//using static OrderHub.Application.DTOs.ProductDtos;

//namespace OrderHub.Application.Queries;

//public static class ProductQueries
//{
//    //public record GetAllProductsQuery() : IRequest<IEnumerable<ProductListDto>>;
//    //public record GetAllProductsPagedQuery(int PageNumber = 1, int PageSize = 20) : IRequest<PagedResult<ProductListDto>>;
//    public record GetProductsByNameQuery(string SearchTerm) : IRequest<IEnumerable<ProductListDto>>;
//    //public record GetProductForEditQuery(int Id) : IRequest<ProductFormDto>;
//    public record GetProductsByCategoryQuery(int CategoryId) : IRequest<IEnumerable<ProductListDto>>;
//}
