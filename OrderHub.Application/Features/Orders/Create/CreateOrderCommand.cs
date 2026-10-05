using MediatR;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Domain.Common;
using System;

namespace OrderHub.Application.Features.Orders.Create;

public record CreateOrderCommand(Guid? OrderDraftId, OrderInput.Order Order) : IRequest<Result<int>>;