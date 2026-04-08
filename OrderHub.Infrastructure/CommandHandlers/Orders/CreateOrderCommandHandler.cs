using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure.Helpers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.OrderCommands;
using static OrderHub.Application.DTOs.OrderDtos;
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
        order.PaymentMethodId = request.CreateDto.PaymentMothodId;

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

            foreach (OrderItemAttributeDto attributeDto in orderItemDto.Attributes ?? Enumerable.Empty<OrderItemAttributeDto>())
            {
                orderItem.AddAttribute(new OrderItemAttribute(attributeDto.Name, attributeDto.Value));
            }

            order.AddOrderItem(orderItem);
        }

        foreach (OrderDeliveryStepCreateDto deliveryStepDto in request.CreateDto.DeliverySteps ?? Enumerable.Empty<OrderDeliveryStepCreateDto>())
        {
            order.AddDeliveryStep(new OrderDeliveryStep
            {
                StepOrder = deliveryStepDto.StepOrder,
                DeliveryMethod = deliveryStepDto.DeliveryMethod,
                DeliverymanId = deliveryStepDto.DeliveryMethod == Domain.Enums.DeliveryMethod.DeliveryMan
                    ? deliveryStepDto.HandlerId
                    : null,
                ShippingCarrierId = deliveryStepDto.DeliveryMethod == Domain.Enums.DeliveryMethod.ShippingCompany
                    ? deliveryStepDto.HandlerId
                    : null
            });
        }

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
        return $"ORD-{DateTime.Now.ToString("yyyyMMdd")}-{number.ToString().PadLeft(4, '0')}";
    }
}
