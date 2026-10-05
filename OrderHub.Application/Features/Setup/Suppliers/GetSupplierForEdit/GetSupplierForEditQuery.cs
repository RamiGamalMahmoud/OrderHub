using MediatR;
using System.Collections.Generic;
using OrderHub.Application.Features.Setup.Suppliers.Contracts;

namespace OrderHub.Application.Features.Setup.Suppliers;

public record GetSupplierForEditQuery(int Id) : IRequest<SupplierEdit>;
