using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.OrderCommands;
using static OrderHub.Application.DTOs.OrderItemDtos;

namespace OrderHub.Infrastructure.CommandHandlers.Orders;

internal class CreateOrderCommandHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<CreateOrderCommand, Result<int>>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result<int>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        string orderNumber = GenerateOrderNumber(await GetNextOrderNumber(appDbContext));

        Order order = new Order(request.CreateDto.ClientId, orderNumber);
        order.DeliveryMethod = request.CreateDto.DeliveryMethod;
        order.DeliverymanId = request.CreateDto.DeliveryManId;
        order.ShippingCarrierId = request.CreateDto.ShippingCarrierId;

        foreach(OrderItemDto orderItemDto in request.CreateDto.OrderItems)
        {
            OrderItem orderItem = new OrderItem(
                orderItemDto.ProductId, 
                orderItemDto.ProductName, 
                0, 
                orderItemDto.UnitPrice, 
                orderItemDto.Quantity,
                orderItemDto.SupplierName,
                orderItemDto.SupplierId);
            order.AddOrderItem(orderItem);
        }

        appDbContext.Orders.Add(order);
        await appDbContext.SaveChangesAsync();
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
        return $"ORD-{DateTime.Now.ToString("yyyyMMdd")}-{number.ToString().PadLeft(4, '0')}";
    }
}
