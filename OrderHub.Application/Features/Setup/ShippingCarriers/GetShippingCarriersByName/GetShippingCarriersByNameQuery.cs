using MediatR;
using System.Collections.Generic;
using OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;

namespace OrderHub.Application.Features.Setup.ShippingCarriers;

public record GetShippingCarriersByNameQuery(string SearchTerm = null, int Take = 20) : IRequest<IEnumerable<ShippingCarrierListItem>>;
