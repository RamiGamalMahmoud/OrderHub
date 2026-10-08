using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Domain.Common;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Products.Update;

[RequirePermission(PermissionFeature.Products, PermissionLevel.Write)]
public record UpdateProductCommand(int id, ProductUpdateInput ProductUpdateInput) : IRequest<Result<int>>;
