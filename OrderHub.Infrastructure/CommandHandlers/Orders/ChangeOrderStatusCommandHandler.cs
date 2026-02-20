using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.OrderCommands;

namespace OrderHub.Infrastructure.CommandHandlers.Orders;

internal class ChangeOrderStatusCommandHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<ChangeOrderStatusCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(ChangeOrderStatusCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
        OrderStatus orderStatus = await appDbContext.OrderStatuses.SingleOrDefaultAsync(o => o.Id == request.OrderStatusId, cancellationToken: cancellationToken);
        Order order = await appDbContext.Orders.SingleOrDefaultAsync(o => o.Id == request.Id, cancellationToken: cancellationToken);
        if (order == null)
        {
            return Result.Failure("Order not found.");
        }
        order.ChangeOrderStatus(orderStatus);
        await appDbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
