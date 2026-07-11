using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure.Helpers;
using OrderHub.Infrastructure.Orders;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.OrderCommands;

namespace OrderHub.Infrastructure.CommandHandlers.Orders;

internal class UpdateOrderCommandHandler(AppDbContextFactory appDbContextFactory, OrderWriteService orderWriteService)
    : IRequestHandler<UpdateOrderCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;
    private readonly OrderWriteService _orderWriteService = orderWriteService;

    public async Task<Result> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        Order order = await appDbContext.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.DeliverySteps)
            .Include(o => o.EntitySequences)
            .SingleOrDefaultAsync(o => o.Id == request.UpdateDto.Id, cancellationToken);

        if (order is null)
        {
            return Result.Failure("الطلب غير موجود.");
        }

        _orderWriteService.Update(appDbContext, order, request.UpdateDto);
        await OrderEntitySequenceManager.SyncAsync(appDbContext, order, cancellationToken);
        await appDbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
