using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure.Helpers;
using OrderHub.Infrastructure.Orders;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.OrderCommands;

namespace OrderHub.Infrastructure.CommandHandlers.Orders;

internal class CreateOrderCommandHandler(AppDbContextFactory appDbContextFactory, OrderWriteService orderWriteService) : IRequestHandler<CreateOrderCommand, Result<int>>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;
    private readonly OrderWriteService _orderWriteService = orderWriteService;

    public CreateOrderCommandHandler(AppDbContextFactory appDbContextFactory)
        : this(appDbContextFactory, new OrderWriteService())
    {
    }

    public async Task<Result<int>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        string orderNumber = GenerateOrderNumber(await GetNextOrderNumber(appDbContext));
        Order order = _orderWriteService.Create(request.CreateDto, orderNumber);

        appDbContext.Orders.Add(order);
        await OrderEntitySequenceManager.SyncAsync(appDbContext, order, cancellationToken);
        await appDbContext.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(order.Id);
    }

    private async Task<int> GetNextOrderNumber(AppDbContext appDbContext)
    {
        string pattern = $"%-{DateTime.Now:yyyyMMdd}-%";

        string lastOrderNumber = await appDbContext
            .Orders
            .Where(o => EF.Functions.Like(o.OrderNumber, pattern))
            .OrderByDescending(o => o.OrderNumber)
            .Select(o => o.OrderNumber)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(lastOrderNumber))
            return 1;

        string numberPart = lastOrderNumber.Split('-').LastOrDefault();

        int number = int.Parse(numberPart);

        return number + 1;
    }

    private string GenerateOrderNumber(int number)
    {
        return $"ORD-{DateTime.Now:yyyyMMdd}-{number.ToString().PadLeft(4, '0')}";
    }
}
