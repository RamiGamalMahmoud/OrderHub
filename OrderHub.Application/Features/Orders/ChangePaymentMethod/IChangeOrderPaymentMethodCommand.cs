using MediatR;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Orders.ChangePaymentMethod;

public interface IChangeOrderPaymentMethodCommand : IRequest<Result>;
