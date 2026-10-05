using MediatR;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.GetForEdit;

public record GetOrderForEditQuery(int OrderId) : IRequest<OrderEditorData>;
