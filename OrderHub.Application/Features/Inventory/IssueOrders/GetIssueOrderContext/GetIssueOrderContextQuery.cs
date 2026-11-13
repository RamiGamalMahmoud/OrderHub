using MediatR;
using OrderHub.Application.Features.Inventory.IssueOrders.Contracts;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Inventory.IssueOrders.GetIssueOrderContext;

public sealed record GetIssueOrderContextQuery(int? OrderId = null)
    : IRequest<Result<IssueOrderContext>>;
