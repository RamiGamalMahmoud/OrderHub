using MediatR;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using OrderHub.Domain.Enums;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Interfaces.Repositories;

namespace OrderHub.Application.Features.Orders.Update;

internal class UpdateOrderHandler : IRequestHandler<UpdateOrderCommand, Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateOrderHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetForUpdateAsync(request.OrderId, cancellationToken);
        if (order is null)
            return Result.Failure("الطلب غير موجود.");
        if (request.Order is null)
            return Result.Failure("بيانات الطلب غير موجودة.");

        var prepared = BuildReplacement(order, request.Order);
        if (!prepared.IsSuccess)
            return Result.Failure(prepared.ErrorMessage);
        var input = request.Order;
        order.ReplaceDetails(prepared.Value.OrderItems, prepared.Value.DeliverySteps);
        order.UpdateHeader(input.ClientId, input.OrderSource, input.DeliveryMethod,
            input.DeliveryMethod == DeliveryMethod.DeliveryMan ? input.DeliveryManId : null,
            input.DeliveryMethod == DeliveryMethod.ShippingCompany ? input.ShippingCarrierId : null,
            input.PaymentMethodId);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (PersistenceException)
        {
            return Result.Failure("تعذر حفظ الطلب. تحقق من البيانات المحددة ثم حاول مرة أخرى.");
        }
    }

    private static Result<Order> BuildReplacement(Order order, OrderInput.Order input)
    {
        if (!Enum.IsDefined(input.DeliveryMethod) || !Enum.IsDefined(input.OrderSource))
            return Result<Order>.Failure("مصدر الطلب أو طريقة التوصيل غير صالح.");
        if (input.DeliveryMethod == DeliveryMethod.DeliveryMan && !input.DeliveryManId.HasValue ||
            input.DeliveryMethod == DeliveryMethod.ShippingCompany && !input.ShippingCarrierId.HasValue)
            return Result<Order>.Failure("يجب اختيار جهة التوصيل.");

        var replacement = new Order(input.ClientId, order.OrderNumber);
        var steps = input.DeliveryMethod == DeliveryMethod.DeliveryChain
            ? (input.DeliverySteps ?? Enumerable.Empty<OrderInput.DeliveryStep>()).ToList()
            : new List<OrderInput.DeliveryStep>();
        if (steps.Any(step => step.DeliveryMethod is not (DeliveryMethod.DeliveryMan or DeliveryMethod.ShippingCompany) ||
                step.StepOrder <= 0) || steps.Select(step => step.StepOrder).Distinct().Count() != steps.Count)
            return Result<Order>.Failure("خطوات التوصيل أو ترتيبها غير صالح.");
        foreach (var item in input.OrderItems ?? Enumerable.Empty<OrderInput.Item>())
        {
            var result = replacement.AddOrderItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity,
                item.VatRate, item.SupplierName, item.SupplierId,
                item.Properties.Select(property => new OrderItemPropertyData(property.PropertyId, property.Value)).ToList().AsReadOnly());
            if (!result.IsSuccess)
                return Result<Order>.Failure(result.ErrorMessage);
        }
        if (replacement.OrderItems.Count == 0)
            return Result<Order>.Failure("يجب إضافة منتج واحد على الأقل.");
        foreach (var step in steps.OrderBy(step => step.StepOrder))
        {
            replacement.AddDeliveryStep(new OrderDeliveryStep
            {
                StepOrder = step.StepOrder,
                DeliveryMethod = step.DeliveryMethod,
                DeliverymanId = step.DeliveryMethod == DeliveryMethod.DeliveryMan ? step.HandlerId : null,
                ShippingCarrierId = step.DeliveryMethod == DeliveryMethod.ShippingCompany ? step.HandlerId : null
            });
        }
        return replacement;
    }
}
