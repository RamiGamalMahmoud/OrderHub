using MediatR;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Orders.Update;

public record UpdateOrderCommand(int OrderId, OrderInput.Order Order) : IRequest<Result>;
