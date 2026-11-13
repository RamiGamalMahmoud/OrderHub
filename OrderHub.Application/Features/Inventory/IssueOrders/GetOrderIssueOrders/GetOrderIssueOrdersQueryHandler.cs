using MediatR;
using OrderHub.Application.Features.Inventory.IssueOrders.Contracts;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.IssueOrders.GetOrderIssueOrders;

internal sealed class GetOrderIssueOrdersQueryHandler(IStockIssueOrderRepository issueOrders)
    : IRequestHandler<GetOrderIssueOrdersQuery, Result<IReadOnlyList<StockIssueOrderDetails>>>
{
    public async Task<Result<IReadOnlyList<StockIssueOrderDetails>>> Handle(
        GetOrderIssueOrdersQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<StockIssueOrder> orders = await issueOrders.GetForOrderAsync(
            request.OrderId,
            cancellationToken);

        IReadOnlyList<StockIssueOrderDetails> details = orders
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id)
            .Select(StockIssueOrderMapper.ToDetails)
            .ToList();

        return Result<IReadOnlyList<StockIssueOrderDetails>>.Success(details);
    }
}
