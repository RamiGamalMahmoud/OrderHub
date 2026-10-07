using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Common;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Orders.Lifecycle;

[RequirePermission(PermissionFeature.Orders, PermissionLevel.Read)]
public sealed record GetOrderWorkflowQuery(int OrderId) : IRequest<OrderWorkflowState>;

[RequirePermission(PermissionFeature.Orders, PermissionLevel.Read)]
public sealed record GetOrderHistoryQuery(int OrderId, int? StepId = null, int PageNumber = 1, int PageSize = 20)
    : IRequest<PagedResult<OrderHistoryEntry>>;

internal sealed class OrderWorkflowQueryHandlers(IOrderReader reader) :
    IRequestHandler<GetOrderWorkflowQuery, OrderWorkflowState>,
    IRequestHandler<GetOrderHistoryQuery, PagedResult<OrderHistoryEntry>>
{
    public Task<OrderWorkflowState> Handle(GetOrderWorkflowQuery request, CancellationToken cancellationToken) =>
        reader.GetWorkflowAsync(request.OrderId, cancellationToken);

    public Task<PagedResult<OrderHistoryEntry>> Handle(GetOrderHistoryQuery request, CancellationToken cancellationToken) =>
        reader.GetHistoryAsync(request.OrderId, request.StepId, request.PageNumber, request.PageSize, cancellationToken);
}
