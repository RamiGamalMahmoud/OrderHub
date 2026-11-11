using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Application.Common;
using OrderHub.Application.Features.Products.Contracts;

namespace OrderHub.Application.Features.Products.Search;

[RequirePermission(PermissionFeature.Products, PermissionLevel.Read)]
public sealed record SearchProductsQuery(string SearchTerm, int Page = 1, int PageSize = 10)
    : IRequest<PagedResult<ProductLookupItem>>;
