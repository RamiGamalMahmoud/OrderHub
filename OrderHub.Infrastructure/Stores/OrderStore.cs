using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure.Helpers;
using OrderHub.Infrastructure.Orders;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Stores;

internal class OrderStore : IOrderStore
{
    private readonly AppDbContextFactory _dbContextFactory;
    private readonly OrderWriteService _orderWriteService;

    public OrderStore(AppDbContextFactory dbContextFactory, OrderWriteService orderWriteService)
    {
        _dbContextFactory = dbContextFactory;
        _orderWriteService = orderWriteService;
    }

    public async Task<Result<int>> Create(OrderDetails.Order dto, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _dbContextFactory.CreateDbContext();

        string orderNumber = GenerateOrderNumber(await GetNextOrderNumber(appDbContext));

        Order order = new(dto.ClientId, orderNumber);

        order.ChangeClient(dto.ClientId);
        order.ChangeDeliveryMethod(dto.DeliveryMethod);
        order.ChangeDeliveryman(dto.DeliveryManId);
        order.ChangeShippingCarrier(dto.ShippingCarrierId);
        order.ChangePaymentMethod(dto.PaymentMothodId);

        foreach (OrderDetails.Item item in dto.OrderItems ?? Enumerable.Empty<OrderDetails.Item>())
        {
            Result<OrderItem> result = order.AddOrderItem(
                item.ProductId,
                item.ProductName,
                item.UnitPrice,
                item.Quantity,
                item.SupplierName,
                item.SupplierId,
                item.Properties.Select(p => new OrderItemPropertyData( p.PropertyId, p.Value)).ToList().AsReadOnly()
                );

            if (!result.IsSuccess)
                return Result<int>.Failure(result.ErrorMessage);

            OrderItem orderItem = result.Value;
        }

        foreach (OrderDetails.DeliveryStep stepDto in dto.DeliverySteps ?? Enumerable.Empty<OrderDetails.DeliveryStep>())
        {
            order.AddDeliveryStep(new OrderDeliveryStep
            {
                StepOrder = stepDto.StepOrder,
                DeliveryMethod = stepDto.DeliveryMethod,
                DeliverymanId = stepDto.DeliveryMethod == DeliveryMethod.DeliveryMan
                    ? stepDto.HandlerId
                    : null,
                ShippingCarrierId = stepDto.DeliveryMethod == DeliveryMethod.ShippingCompany
                    ? stepDto.HandlerId
                    : null
            });
        }

        appDbContext.Orders.Add(order);

        await OrderEntitySequenceManager.SyncAsync(appDbContext, order, cancellationToken);

        await appDbContext.SaveChangesAsync(cancellationToken);

        return order.Id;
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

    public async Task<Result> UpdateOrder(int orderId, OrderDetails.Order order, CancellationToken cancellationToken = default)
    {
        using AppDbContext appDbContext = _dbContextFactory.CreateDbContext();

        Order storedOrder = await appDbContext.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.DeliverySteps)
            .Include(o => o.EntitySequences)
            .SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure("الطلب غير موجود.");
        }

        _orderWriteService.Update(appDbContext, storedOrder, order);
        await OrderEntitySequenceManager.SyncAsync(appDbContext, storedOrder, cancellationToken);
        await appDbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> Delete(int orderId)
    {
        using AppDbContext dbContext = _dbContextFactory.CreateDbContext();
        Order order = await dbContext.Orders.SingleOrDefaultAsync(o => o.Id == orderId);
        try
        {
            dbContext.Orders.Remove(order);
            await dbContext.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
