using MediatR;
using OrderHub.Domain.Common;
using static OrderHub.Application.DTOs.ShippingCarriersDtos;

namespace OrderHub.Application.Commands;

public static class ShippingCarriersCommands
{
    public record CreateShippingCarrierCommand(ShippingCarrierCreateDto Dto) : IRequest<Result>;
    public record DeleteShippingCarrierCommand(int Id) : IRequest<Result>;
    public record UpdateShippingCarrierCommand(ShippingCarrierUpdateDto Dto) : IRequest<Result>;
}
