using MediatR;
using System.Collections.Generic;
using static OrderHub.Application.DTOs.SupplierDtos;

namespace OrderHub.Application.Queries;

public static class SupplierQueries
{
    public record GetAllSuppliersQuery() : IRequest<IEnumerable<SupplierListDto>>;
    public record GetSupplierForEditQuery(int Id) : IRequest<SupplierEditDto>;
}
