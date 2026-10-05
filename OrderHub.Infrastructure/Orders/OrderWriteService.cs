using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderHub.Infrastructure.Orders;

internal class OrderWriteService
{

    public Result Update(AppDbContext appDbContext, Order order, OrderInput.Order orderDetials)
    {
        if (!Enum.IsDefined(orderDetials.DeliveryMethod) || !Enum.IsDefined(orderDetials.OrderSource))
            return Result.Failure("مصدر الطلب أو طريقة التوصيل غير صالح.");
        if (orderDetials.DeliveryMethod == DeliveryMethod.DeliveryMan && !orderDetials.DeliveryManId.HasValue ||
            orderDetials.DeliveryMethod == DeliveryMethod.ShippingCompany && !orderDetials.ShippingCarrierId.HasValue)
            return Result.Failure("يجب اختيار جهة التوصيل.");

        // Domain reconstruction happens on an untracked aggregate. Failure leaves both the stored
        // aggregate and its tracked children untouched, even if this context is subsequently saved.
        var replacement = new Order(orderDetials.ClientId, order.OrderNumber);
        var steps = orderDetials.DeliveryMethod == DeliveryMethod.DeliveryChain
            ? (orderDetials.DeliverySteps ?? Enumerable.Empty<OrderInput.DeliveryStep>()).ToList()
            : new List<OrderInput.DeliveryStep>();
        if (steps.Any(step => step.DeliveryMethod is not (DeliveryMethod.DeliveryMan or DeliveryMethod.ShippingCompany) ||
                step.StepOrder <= 0) || steps.Select(step => step.StepOrder).Distinct().Count() != steps.Count)
            return Result.Failure("خطوات التوصيل أو ترتيبها غير صالح.");
        var result = PopulateDetails(replacement, orderDetials.OrderItems, steps.OrderBy(step => step.StepOrder));
        if (!result.IsSuccess)
            return result;
        if (replacement.OrderItems.Count == 0)
            return Result.Failure("يجب إضافة منتج واحد على الأقل.");

        appDbContext.OrderItems.RemoveRange(order.OrderItems);
        appDbContext.OrderDeliverySteps.RemoveRange(order.DeliverySteps);
        order.ReplaceDetails(replacement.OrderItems, replacement.DeliverySteps);
        order.UpdateHeader(orderDetials.ClientId, orderDetials.OrderSource, orderDetials.DeliveryMethod,
            orderDetials.DeliveryMethod == DeliveryMethod.DeliveryMan ? orderDetials.DeliveryManId : null,
            orderDetials.DeliveryMethod == DeliveryMethod.ShippingCompany ? orderDetials.ShippingCarrierId : null,
            orderDetials.PaymentMethodId);
        return Result.Success();
    }

    private static Result PopulateDetails(
        Order order,
        IEnumerable<OrderInput.Item> orderItems,
        IEnumerable<OrderInput.DeliveryStep> deliverySteps)
    {
        foreach (OrderInput.Item item in orderItems ?? Enumerable.Empty<OrderInput.Item>())
        {
            var result = order.AddOrderItem(
                item.ProductId,
                item.ProductName,
                item.UnitPrice,
                item.Quantity,
                item.VatRate,
                item.SupplierName,
                item.SupplierId,
                item.Properties.Select(property => new OrderItemPropertyData(
                    property.PropertyId,
                    property.Value))
                    .ToList()
                    .AsReadOnly());
            if (!result.IsSuccess)
                return Result.Failure(result.ErrorMessage);
        }
        foreach (OrderInput.DeliveryStep deliveryStep in deliverySteps ?? Enumerable.Empty<OrderInput.DeliveryStep>())
        {
            order.AddDeliveryStep(BuildDeliveryStep(order.Id, deliveryStep));
        }
        return Result.Success();
    }

    private static OrderDeliveryStep BuildDeliveryStep(int orderId, OrderInput.DeliveryStep deliveryStep)
    {
        return new OrderDeliveryStep
        {
            OrderId = orderId,
            StepOrder = deliveryStep.StepOrder,
            DeliveryMethod = deliveryStep.DeliveryMethod,
            DeliverymanId = deliveryStep.DeliveryMethod == DeliveryMethod.DeliveryMan
                ? deliveryStep.HandlerId
                : null,
            ShippingCarrierId = deliveryStep.DeliveryMethod == DeliveryMethod.ShippingCompany
                ? deliveryStep.HandlerId
                : null
        };
    }
}
