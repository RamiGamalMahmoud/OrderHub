using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Products.Index;

[RequirePermission(PermissionFeature.Products, PermissionLevel.Read)]
public sealed record GetProductIndexQuery(string Search = null, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<ProductIndexItem>>;

public sealed record ProductIndexItem(int Id, string Name, string Code, decimal Price,
    string CategoryName, int SuppliersCount, int PropertiesCount);
