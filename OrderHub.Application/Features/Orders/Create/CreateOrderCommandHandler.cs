using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Application.Features.Orders.Allocations;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.Models.CommercialDocuments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.Create;

internal class CreateOrderCommandHandler
    : IRequestHandler<CreateOrderCommand, Result<int>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IQuotationRepository _quotationRepository;
    private readonly IProformaInvoiceRepository _proformaInvoiceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IDraftRepository _draftRepository;
    private readonly INumberSequenceService _numberSequenceService;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IWarehouseRepository _warehouseRepository;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IQuotationRepository quotationRepository,
        IProformaInvoiceRepository proformaInvoiceRepository,
        ICurrentUser currentUser,
        IDraftRepository draftRepository,
        INumberSequenceService numberSequenceService,
        IInventoryRepository inventoryRepository,
        IWarehouseRepository warehouseRepository)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _quotationRepository = quotationRepository;
        _proformaInvoiceRepository = proformaInvoiceRepository;
        _currentUser = currentUser;
        _draftRepository = draftRepository;
        _numberSequenceService = numberSequenceService;
        _inventoryRepository = inventoryRepository;
        _warehouseRepository = warehouseRepository;
    }

    public async Task<Result<int>> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        var transactionActive = false;

        try
        {
            var input = request.Order;

            if (input is null)
                return Result<int>.Failure("بيانات الطلب غير موجودة.");

            if (!_currentUser.IsAuthenticated || _currentUser.Id is not > 0)
                return Result<int>.Failure("يجب تسجيل الدخول لإنشاء الطلب.");

            if (request.CreationRequestId == Guid.Empty)
                return Result<int>.Failure("معرف عملية الإنشاء غير صالح.");

            if (!Enum.IsDefined(input.DeliveryMethod) ||
                !Enum.IsDefined(input.OrderSource))
            {
                return Result<int>.Failure(
                    "مصدر الطلب أو طريقة التوصيل غير صالح.");
            }

            if (input.DeliveryMethod == DeliveryMethod.DeliveryMan &&
                !input.DeliveryManId.HasValue ||
                input.DeliveryMethod == DeliveryMethod.ShippingCompany &&
                !input.ShippingCarrierId.HasValue)
            {
                return Result<int>.Failure("يجب اختيار جهة التوصيل.");
            }

            var steps = input.DeliveryMethod == DeliveryMethod.DeliveryChain
                ? (input.DeliverySteps ??
                   Enumerable.Empty<OrderInput.DeliveryStep>()).ToList()
                : [];

            if (steps.Any(step =>
                    step is null ||
                    step.DeliveryMethod is not (
                        DeliveryMethod.DeliveryMan or
                        DeliveryMethod.ShippingCompany) ||
                    step.StepOrder <= 0 ||
                    step.HandlerId <= 0) ||
                steps.Select(step => step.StepOrder)
                    .Distinct()
                    .Count() != steps.Count)
            {
                return Result<int>.Failure(
                    "خطوات التوصيل أو ترتيبها غير صالح.");
            }

            var inputItems = (input.OrderItems ?? Enumerable.Empty<OrderInput.Item>()).ToList();
            if (inputItems.Any(item =>
                    item is null ||
                    (item.Properties ?? Enumerable.Empty<OrderInput.Property>())
                        .Any(property => property is null)))
            {
                return Result<int>.Failure(
                    "بنود الطلب أو خصائصها غير صالحة.");
            }

            var plan = OrderAllocationRules.Plan(inputItems, allowExistingIds: false);
            if (!plan.IsSuccess)
                return Result<int>.Failure(plan.ErrorMessage);

            var warehouses = await OrderAllocationReservation.LoadWarehousesAsync(
                _warehouseRepository,
                plan.Value.SelectMany(item => item.Allocations)
                    .Where(allocation => allocation.SourceKind == AllocationSourceKind.Warehouse)
                    .Select(allocation => allocation.WarehouseId.Value),
                cancellationToken);
            if (!warehouses.IsSuccess)
                return Result<int>.Failure(warehouses.ErrorMessage);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            transactionActive = true;

            var plannedReservations = plan.Value
                .SelectMany(item => OrderAllocationReservation.WarehouseReservations(
                    item.Input.ProductId, item.Allocations))
                .ToList();
            if (plannedReservations.Count > 0)
            {
                await _inventoryRepository.LockBalancesAsync(
                    plannedReservations
                        .Select(item => (item.WarehouseId, item.ProductId))
                        .ToList(),
                    cancellationToken);

                var reserved = await OrderAllocationReservation.ReserveAsync(
                    _inventoryRepository,
                    plannedReservations,
                    warehouses.Value,
                    cancellationToken);
                if (!reserved.IsSuccess)
                    return Result<int>.Failure(reserved.ErrorMessage, reserved.ErrorCode);
            }

            long nextOrderNumber = await _numberSequenceService.NextGlobalAsync(NumberSequenceKind.Order, cancellationToken);

            // Check after allocation lock so simultaneous retries see
            // the first committed request.
            if (request.CreationRequestId.HasValue)
            {
                var existing =
                    await _orderRepository.FindCreationRequestAsync(
                        request.CreationRequestId.Value,
                        _currentUser.Id.Value,
                        cancellationToken);

                if (existing.HasValue)
                    return existing.Value;
            }

            string orderNumber = $"ORD-{nextOrderNumber:D5}";

            Order order = new(
                request.Order.ClientId,
                orderNumber,
                request.CreationRequestId);

            order.UpdateHeader(
                request.Order.ClientId,
                request.Order.OrderSource,
                request.Order.DeliveryMethod,
                input.DeliveryMethod == DeliveryMethod.DeliveryMan
                    ? input.DeliveryManId
                    : null,
                input.DeliveryMethod == DeliveryMethod.ShippingCompany
                    ? input.ShippingCarrierId
                    : null,
                request.Order.PaymentMethodId);

            var initial = order.InitializeHistory(
                new OrderOperation(
                    _currentUser.Id.Value,
                    _currentUser.DisplayName ?? _currentUser.UserName,
                    order.CreatedAt));

            if (!initial.IsSuccess)
                return Result<int>.Failure(initial.ErrorMessage);

            foreach (PlannedItem plannedItem in plan.Value)
            {
                var item = plannedItem.Input;
                Result<OrderItem> result = order.AddOrderItem(
                    item.ProductId,
                    item.ProductName,
                    item.UnitPrice,
                    item.Quantity,
                    item.VatRate,
                    plannedItem.SupplierName,
                    plannedItem.SupplierId,
                    (item.Properties ?? Enumerable.Empty<OrderInput.Property>())
                        .Select(p => new OrderItemPropertyData(
                            p.PropertyId,
                            p.Value))
                        .ToList()
                        .AsReadOnly());

                if (!result.IsSuccess)
                    return Result<int>.Failure(result.ErrorMessage);

                foreach (PlannedAllocation plannedAllocation in plannedItem.Allocations)
                {
                    Result<OrderInventoryAllocation> allocation = OrderInventoryAllocation.Create(
                        item.ProductId,
                        plannedAllocation.SourceKind,
                        plannedAllocation.Qty,
                        plannedAllocation.WarehouseId,
                        plannedAllocation.SupplierId);
                    if (!allocation.IsSuccess)
                        return Result<int>.Failure(allocation.ErrorMessage);

                    order.AddAllocation(allocation.Value, result.Value);
                }
            }

            if (order.OrderItems.Count == 0)
                return Result<int>.Failure("يجب إضافة منتج واحد على الأقل.");

            var financials = order.SetFinancialAdjustments(input.DeliveryCost, input.DiscountType, input.DiscountValue);
            if (!financials.IsSuccess) return Result<int>.Failure(financials.ErrorMessage);

            foreach (OrderInput.DeliveryStep step in
                steps.OrderBy(step => step.StepOrder))
            {
                order.AddDeliveryStep(new OrderDeliveryStep
                {
                    StepOrder = step.StepOrder,
                    DeliveryMethod = step.DeliveryMethod,

                    DeliverymanId =
                        step.DeliveryMethod == DeliveryMethod.DeliveryMan
                            ? step.HandlerId
                            : null,

                    ShippingCarrierId =
                        step.DeliveryMethod == DeliveryMethod.ShippingCompany
                            ? step.HandlerId
                            : null
                });
            }

            // 3. Ensure entity sequences

            // 4. Add order
            _orderRepository.Add(order);

            if (request.OrderDraftId is Guid draftReference)
            {
                int? userId = _currentUser.Id;

                var quotations =
                    await _quotationRepository.GetByDraftReference(
                        draftReference,
                        cancellationToken);

                foreach (Quotation quotation in quotations)
                {
                    quotation.LinkToOrder(order);
                }

                var proformaInvoices =
                    await _proformaInvoiceRepository.GetByDraftReference(
                        draftReference,
                        cancellationToken);

                foreach (ProformaInvoice proformaInvoice in proformaInvoices)
                {
                    proformaInvoice.LinkToOrder(order);
                }

                await _draftRepository.StageDeleteAsync(
                    draftReference,
                    userId.Value,
                    cancellationToken);
            }

            // 5. Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            transactionActive = false;

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return order.Id;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (PersistenceException)
        {
            return Result<int>.Failure(
                "تعذر حفظ الطلب. تحقق من البيانات ثم حاول مرة أخرى.");
        }
        catch (Exception)
        {
            return Result<int>.Failure(
                "تعذر إنشاء الطلب. راجع البيانات ثم حاول مرة أخرى.");
        }
        finally
        {
            if (transactionActive)
            {
                await _unitOfWork.RollbackTransactionAsync(
                    CancellationToken.None);
            }
        }
    }
}
