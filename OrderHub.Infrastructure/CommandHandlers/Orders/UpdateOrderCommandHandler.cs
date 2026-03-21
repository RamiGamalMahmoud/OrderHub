using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.OrderCommands;
using static OrderHub.Application.DTOs.OrderDtos;
using static OrderHub.Application.DTOs.OrderItemDtos;

namespace OrderHub.Infrastructure.CommandHandlers.Orders;

internal class UpdateOrderCommandHandler(AppDbContextFactory appDbContextFactory)
    : IRequestHandler<UpdateOrderCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        Order order = await appDbContext.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.DeliverySteps)
            .SingleOrDefaultAsync(o => o.Id == request.UpdateDto.Id, cancellationToken);

        if (order is null)
        {
            return Result.Failure("الطلب غير موجود.");
        }

        order.ChangeClient(request.UpdateDto.ClientId);
        order.DeliveryMethod = request.UpdateDto.DeliveryMethod;
        order.DeliverymanId = request.UpdateDto.DeliveryManId;
        order.ShippingCarrierId = request.UpdateDto.ShippingCarrierId;
        order.PaymentMethodId = request.UpdateDto.PaymentMothodId;

        appDbContext.OrderItems.RemoveRange(order.OrderItems);
        appDbContext.OrderDeliverySteps.RemoveRange(order.DeliverySteps);
        order.ClearOrderItems();
        order.ClearDeliverySteps();

        foreach (OrderItemDto item in request.UpdateDto.OrderItems)
        {
            order.AddOrderItem(new OrderItem(
                item.ProductId,
                item.ProductName,
                order.Id,
                item.UnitPrice,
                item.Quantity,
                item.SupplierName,
                item.SupplierId));
        }

        foreach (OrderDeliveryStepCreateDto step in request.UpdateDto.DeliverySteps ?? Enumerable.Empty<OrderDeliveryStepCreateDto>())
        {
            order.AddDeliveryStep(new OrderDeliveryStep
            {
                OrderId = order.Id,
                StepOrder = step.StepOrder,
                DeliveryMethod = step.DeliveryMethod,
                DeliverymanId = step.DeliveryMethod == Domain.Enums.DeliveryMethod.DeliveryMan
                    ? step.HandlerId
                    : null,
                ShippingCarrierId = step.DeliveryMethod == Domain.Enums.DeliveryMethod.ShippingCompany
                    ? step.HandlerId
                    : null
            });
        }

        await appDbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
