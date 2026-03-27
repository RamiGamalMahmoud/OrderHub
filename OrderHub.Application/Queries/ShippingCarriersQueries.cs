using MediatR;
using System.Collections.Generic;
using static OrderHub.Application.DTOs.ShippingCarriersDtos;

namespace OrderHub.Application.Queries;

public static class ShippingCarriersQueries
{
    public record GetShippingCarriersQuery : IRequest<IEnumerable<ShippingCarrierListDto>>;
    public record GetShippingCarrierForEditQuery(int Id) : IRequest<ShippingCarrierEditDto>;
    public record GetShippingCarriersByNameQuery(string SearchTerm = null, int Take = 20) : IRequest<IEnumerable<ShippingCarrierListDto>>;
    public record GetShippingCarrierByIdQuery(int Id) : IRequest<ShippingCarrierListDto>;
}
