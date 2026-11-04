using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Setup.Suppliers;

[RequirePermission(PermissionFeature.Suppliers, PermissionLevel.Write)]
public sealed record GetSupplierEditorProductsQuery : IRequest<IEnumerable<ProductInfo>>;
