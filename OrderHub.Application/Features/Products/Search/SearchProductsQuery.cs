using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Application.Features.Products.Contracts;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Products.Search;

[RequirePermission(PermissionFeature.Products, PermissionLevel.Read)]
public record SearchProductsQuery(string SearchTerm) : IRequest<IEnumerable<ProductLookupItem>>;
