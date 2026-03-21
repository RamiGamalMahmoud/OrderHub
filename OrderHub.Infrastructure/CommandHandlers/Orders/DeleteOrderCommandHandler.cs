using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.OrderCommands;

namespace OrderHub.Infrastructure.CommandHandlers.Orders;

internal class DeleteOrderCommandHandler(AppDbContextFactory appDbContextFactory)
    : IRequestHandler<DeleteOrderCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        Order order = await appDbContext.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.DeliverySteps)
            .Include(o => o.OutboxMessages)
            .SingleOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure("الطلب غير موجود.");
        }

        appDbContext.Orders.Remove(order);
        await appDbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
