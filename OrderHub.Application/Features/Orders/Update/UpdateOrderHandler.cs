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

internal class UpdateOrderHandler : IRequestHandler<UpdateOrderCommand, Result<OrderUpdateReceipt>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateOrderHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<OrderUpdateReceipt>> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetForUpdateAsync(request.OrderId, cancellationToken);
        if (order is null)
            return Result<OrderUpdateReceipt>.Failure("الطلب غير موجود.");
        var version = order.CheckVersion(request.ExpectedVersion);
        if (!version.IsSuccess) return Result<OrderUpdateReceipt>.Failure(version.ErrorMessage, "Conflict");
        if (!order.CanEdit) return Result<OrderUpdateReceipt>.Failure("بيانات الطلب مقفولة ولا تقبل التعديل.");
        if (request.Order is null)
            return Result<OrderUpdateReceipt>.Failure("بيانات الطلب غير موجودة.");

        var input = request.Order with { DeliverySteps = request.Order.DeliverySteps?.ToList() };
        var prepared = BuildReplacement(order, input);
        if (!prepared.IsSuccess)
            return Result<OrderUpdateReceipt>.Failure(prepared.ErrorMessage);
        var steps = input.DeliveryMethod == DeliveryMethod.DeliveryChain
            ? (input.DeliverySteps ?? Enumerable.Empty<OrderInput.DeliveryStep>()).OrderBy(step => step.StepOrder).ToList()
            : new List<OrderInput.DeliveryStep>();
        var needsReordering = order.DeliverySteps.Count > 0 &&
            (order.DeliverySteps.Count != steps.Count || steps.Any(step => !step.Id.HasValue ||
                order.DeliverySteps.Single(existing => existing.Id == step.Id.Value).StepOrder != step.StepOrder));
        var transactionActive = false;
        try
        {
            if (needsReordering)
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                transactionActive = true;
                await _orderRepository.PrepareDeliveryStepReorderingAsync(order, cancellationToken);
            }
            order.SynchronizeDeliverySteps(steps.Zip(prepared.Value.DeliverySteps,
                (step, values) => (step.Id, values)).ToList());
            order.ReplaceOrderItems(prepared.Value.OrderItems);
            order.UpdateHeader(input.ClientId, input.OrderSource, input.DeliveryMethod,
                input.DeliveryMethod == DeliveryMethod.DeliveryMan ? input.DeliveryManId : null,
                input.DeliveryMethod == DeliveryMethod.ShippingCompany ? input.ShippingCarrierId : null,
                input.PaymentMethodId);
            order.TouchForEdit();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (transactionActive)
            {
                transactionActive = false;
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            return new OrderUpdateReceipt(order.Version, order.DeliverySteps.OrderBy(step => step.StepOrder)
                .Select(step => new SavedDeliveryStep(step.Id, step.StepOrder)).ToList());
        }
        catch (PersistenceConflictException exception)
        {
            return Result<OrderUpdateReceipt>.Failure(exception.Message, "Conflict");
        }
        catch (PersistenceException)
        {
            return Result<OrderUpdateReceipt>.Failure("تعذر حفظ الطلب. تحقق من البيانات المحددة ثم حاول مرة أخرى.");
        }
        finally
        {
            if (transactionActive)
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
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
        if (steps.Any(step => step is null || step.DeliveryMethod is not (DeliveryMethod.DeliveryMan or DeliveryMethod.ShippingCompany) ||
                step.StepOrder <= 0 || step.HandlerId <= 0) || steps.Select(step => step.StepOrder).Distinct().Count() != steps.Count)
            return Result<Order>.Failure("خطوات التوصيل أو ترتيبها غير صالح.");
        if ((input.OrderItems ?? Enumerable.Empty<OrderInput.Item>()).Any(item => item is null ||
            (item.Properties ?? Enumerable.Empty<OrderInput.Property>()).Any(property => property is null)))
            return Result<Order>.Failure("بنود الطلب أو خصائصها غير صالحة.");
        var ids = steps.Where(step => step.Id.HasValue).Select(step => step.Id.Value).ToList();
        if (ids.Distinct().Count() != ids.Count || ids.Any(id => id <= 0 || !order.DeliverySteps.Any(step => step.Id == id)))
            return Result<Order>.Failure("معرفات خطوات التوصيل غير صالحة لهذا الطلب.");
        foreach (var item in input.OrderItems ?? Enumerable.Empty<OrderInput.Item>())
        {
            var result = replacement.AddOrderItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity,
                item.VatRate, item.SupplierName, item.SupplierId,
                (item.Properties ?? Enumerable.Empty<OrderInput.Property>()).Select(property => new OrderItemPropertyData(property.PropertyId, property.Value)).ToList().AsReadOnly());
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
