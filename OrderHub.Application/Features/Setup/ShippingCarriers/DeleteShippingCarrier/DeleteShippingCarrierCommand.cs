using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;

namespace OrderHub.Application.Features.Setup.ShippingCarriers;

public record DeleteShippingCarrierCommand(int Id) : IRequest<Result>;
