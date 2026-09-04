using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Orders.ChangeStatus;

public interface IChangeOrderStatusCommand : IRequest<Result<OrderStatus>>;
