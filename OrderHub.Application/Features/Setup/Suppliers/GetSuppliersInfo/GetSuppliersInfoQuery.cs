using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using System.Collections.Generic;
using OrderHub.Application.Features.Setup.Suppliers.Contracts;

namespace OrderHub.Application.Features.Setup.Suppliers;

[RequirePermission(PermissionFeature.Suppliers, PermissionLevel.Read)]
public record GetSuppliersInfoQuery : IRequest<IEnumerable<SupplierInfo>>;
