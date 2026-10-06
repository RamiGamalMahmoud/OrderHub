using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Application.Features.Setup.Suppliers.Contracts;

namespace OrderHub.Application.Features.Setup.Suppliers;

[RequirePermission(PermissionFeature.Suppliers, PermissionLevel.Write)]
public record CreateSupplierCommand(SupplierCreateInput Dto) : IRequest<Result>;
