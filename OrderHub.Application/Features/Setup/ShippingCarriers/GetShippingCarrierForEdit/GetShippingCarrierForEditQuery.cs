using MediatR;
using System.Collections.Generic;
using OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;

namespace OrderHub.Application.Features.Setup.ShippingCarriers;

public record GetShippingCarrierForEditQuery(int Id) : IRequest<ShippingCarrierEdit>;
