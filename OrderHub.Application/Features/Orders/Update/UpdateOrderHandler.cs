using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Inventory.Approvals;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.Update;

internal class UpdateOrderHandler : IRequestHandler<UpdateOrderCommand, Result<OrderUpdateReceipt>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IStockIssueOrderRepository _issueOrderRepository;
    private readonly IInventoryApprovalRepository _approvalRepository;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;

    public UpdateOrderHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IInventoryRepository inventoryRepository,
        IWarehouseRepository warehouseRepository,
        IStockIssueOrderRepository issueOrderRepository,
        IInventoryApprovalRepository approvalRepository,
        ICurrentUser currentUser,
        TimeProvider clock)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _inventoryRepository = inventoryRepository;
        _warehouseRepository = warehouseRepository;
        _issueOrderRepository = issueOrderRepository;
        _approvalRepository = approvalRepository;
        _currentUser = currentUser;
        _clock = clock;
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

        var input = request.Order with
        {
            DeliverySteps = request.Order.DeliverySteps?.ToList(),
            OrderItems = request.Order.OrderItems?.ToList()
        };

        var header = ValidateHeader(input);
        if (!header.IsSuccess)
            return Result<OrderUpdateReceipt>.Failure(header.ErrorMessage);

        if (await _approvalRepository.HasPendingEditAsync(order.Id, cancellationToken))
            return Result<OrderUpdateReceipt>.Failure(OrderEditApplier.PendingEditMessage, "Conflict");

        // Edits that could touch issued quantities wait for a manager decision; the payload
        // is stored verbatim and applied atomically once approved.
        var activeIssueOrders = await _issueOrderRepository.GetActiveForOrderAsync(order.Id, cancellationToken);
        if (activeIssueOrders.Count > 0)
            return await EnqueueApprovalAsync(order, input, request.Reason, cancellationToken);

        return await OrderEditApplier.ApplyAsync(
            order,
            input,
            preserveIssued: false,
            manageTransaction: true,
            _orderRepository,
            _inventoryRepository,
            _warehouseRepository,
            _issueOrderRepository,
            _unitOfWork,
            _clock.GetUtcNow().UtcDateTime,
            cancellationToken);
    }

    private async Task<Result<OrderUpdateReceipt>> EnqueueApprovalAsync(
        Order order,
        OrderInput.Order input,
        string reason,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not int userId || userId <= 0)
            return Result<OrderUpdateReceipt>.Failure("يجب تسجيل الدخول لإرسال طلب الاعتماد.", "Unauthorized");

        if (string.IsNullOrWhiteSpace(reason))
            return Result<OrderUpdateReceipt>.Failure(
                "الطلب يحتوي على كميات مصروفة؛ أدخل سبب التعديل ليُرسل إلى المدير للاعتماد.",
                "ApprovalRequired");

        var approval = InventoryApprovalRequest.CreateOrderEdit(
            order.Id,
            order.Version,
            OrderEditPayload.Serialize(input),
            reason,
            userId,
            _clock.GetUtcNow().UtcDateTime);
        if (!approval.IsSuccess)
            return Result<OrderUpdateReceipt>.Failure(approval.ErrorMessage);

        _approvalRepository.Add(approval.Value);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PersistenceConflictException)
        {
            return Result<OrderUpdateReceipt>.Failure(OrderEditApplier.PendingEditMessage, "Conflict");
        }
        catch (PersistenceException)
        {
            return Result<OrderUpdateReceipt>.Failure("تعذر إرسال طلب الاعتماد. حاول مرة أخرى.", "Persistence");
        }

        return new OrderUpdateReceipt(order.Version, [], [], PendingApproval: true);
    }

    private static Result ValidateHeader(OrderInput.Order input)
    {
        if (!Enum.IsDefined(input.DeliveryMethod) || !Enum.IsDefined(input.OrderSource))
            return Result.Failure("مصدر الطلب أو طريقة التوصيل غير صالح.");
        if (input.DeliveryMethod == DeliveryMethod.DeliveryMan && !input.DeliveryManId.HasValue ||
            input.DeliveryMethod == DeliveryMethod.ShippingCompany && !input.ShippingCarrierId.HasValue)
            return Result.Failure("يجب اختيار جهة التوصيل.");

        var steps = input.DeliveryMethod == DeliveryMethod.DeliveryChain
            ? (input.DeliverySteps ?? Enumerable.Empty<OrderInput.DeliveryStep>()).ToList()
            : new List<OrderInput.DeliveryStep>();
        if (steps.Any(step => step is null ||
                step.DeliveryMethod is not (DeliveryMethod.DeliveryMan or DeliveryMethod.ShippingCompany) ||
                step.StepOrder <= 0 || step.HandlerId <= 0) ||
            steps.Select(step => step.StepOrder).Distinct().Count() != steps.Count)
            return Result.Failure("خطوات التوصيل أو ترتيبها غير صالح.");

        if ((input.OrderItems ?? Enumerable.Empty<OrderInput.Item>()).Any(item => item is null ||
                (item.Properties ?? Enumerable.Empty<OrderInput.Property>()).Any(property => property is null)))
            return Result.Failure("بنود الطلب أو خصائصها غير صالحة.");

        return Result.Success();
    }
}
