using MediatR;
using System.Collections.Generic;
using OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;

namespace OrderHub.Application.Features.Setup.ShippingCarriers;

public record GetAllShippingCarriersInfoQuery : IRequest<IEnumerable<ShippingCarrierInfo>>;
