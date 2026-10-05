using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Application.Features.Setup.Suppliers.Contracts;

namespace OrderHub.Application.Features.Setup.Suppliers;

public record UpdateSupplierCommand(SupplierUpdateInput Dto) : IRequest<Result>;
