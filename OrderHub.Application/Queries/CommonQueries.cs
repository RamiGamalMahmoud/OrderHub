using MediatR;
using System.Collections.Generic;
using static OrderHub.Application.DTOs.CommonDtos;

namespace OrderHub.Application.Queries;

public static class CommonQueries
{
    public record GetCategoriesInfoQuery : IRequest<IEnumerable<CategoryInfoDto>>;
    public record GetRootCategoriesQuery : IRequest<IEnumerable<CategoryInfoDto>>;
    public record GetSubCategoriesQuery(int ParentCategoryId) : IRequest<IEnumerable<CategoryInfoDto>>;
    public record GetCitiesInfoQuery : IRequest<IEnumerable<CityInfoDto>>;
    public record GetSuppliersInfoQuery: IRequest<IEnumerable<SupplierInfoDto>>;
    public record GetClientsInfoQuery : IRequest<IEnumerable<ClientInfoDto>>;
    public record GetProductsIfoQuery : IRequest<IEnumerable<ProductInfoDto>>;
    public record GetOrderStautsesInfoQuery : IRequest<IEnumerable<OrderStatusInfoDto>>;
}
