using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using System.Collections.Generic;
using OrderHub.Application.Features.Products.Contracts;

namespace OrderHub.Application.Features.Products;

[RequirePermission(PermissionFeature.Products, PermissionLevel.Read)]
public record GetProductsInfoQuery : IRequest<IEnumerable<ProductInfo>>;
