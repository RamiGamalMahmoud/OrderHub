using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Application.Features.Products.Contracts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Products.Get;

[RequirePermission(PermissionFeature.Products, PermissionLevel.Read)]
public record GetProductQuery(int Id) : IRequest<ProductDetails>;
