using MediatR;

namespace OrderHub.Application.Features.Orders.GetEditorLookups;

public record GetOrderEditorLookupsQuery : IRequest<OrderEditorLookups>;
