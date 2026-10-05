using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Stores;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.Models.CommercialDocuments;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.Create;

internal class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<int>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IQuotationRepository _quotationRepository;
    private readonly IProformaInvoiceRepository _proformaInvoiceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IDraftStore _draftStore;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IQuotationRepository quotationRepository,
        IProformaInvoiceRepository proformaInvoiceRepository,
        ICurrentUser currentUser,
        IDraftStore draftStore)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _quotationRepository = quotationRepository;
        _proformaInvoiceRepository = proformaInvoiceRepository;
        _currentUser = currentUser;
        _draftStore = draftStore;
    }

    public async Task<Result<int>> Handle(
CreateOrderCommand request,
CancellationToken cancellationToken)
    {
        try
        {
            // 1. Validate order
            // ...

            // 2. Create order
            int nextOrderNumber =
                await _orderRepository.GetNextOrderNumberAsync(cancellationToken);

            string orderNumber =
                $"ORD-{DateTime.Now:yyyy-MM-dd}-{nextOrderNumber:D4}";

            Order order = new(
                request.Order.ClientId,
                orderNumber);

            order.UpdateHeader(
                request.Order.ClientId,
                request.Order.OrderSource,
                request.Order.DeliveryMethod,
                request.Order.DeliveryManId,
                request.Order.ShippingCarrierId,
                request.Order.PaymentMethodId
                );

            foreach (OrderInput.Item item in request.Order.OrderItems)
            {
                Result<OrderItem> result = order.AddOrderItem(
                    item.ProductId,
                    item.ProductName,
                    item.UnitPrice,
                    item.Quantity,
                    item.VatRate,
                    item.SupplierName,
                    item.SupplierId,
                    item.Properties
                        .Select(p => new OrderItemPropertyData(
                            p.PropertyId,
                            p.Value))
                        .ToList()
                        .AsReadOnly());

                if (!result.IsSuccess)
                    return Result<int>.Failure(result.ErrorMessage);
            }

            foreach (OrderInput.DeliveryStep step in
                request.Order.DeliverySteps ?? Enumerable.Empty<OrderInput.DeliveryStep>())
            {
                order.AddDeliveryStep(new OrderDeliveryStep
                {
                    StepOrder = step.StepOrder,
                    DeliveryMethod = step.DeliveryMethod,
                    DeliverymanId = step.DeliveryMethod == DeliveryMethod.DeliveryMan
                        ? step.HandlerId
                        : null,
                    ShippingCarrierId = step.DeliveryMethod == DeliveryMethod.ShippingCompany
                        ? step.HandlerId
                        : null
                });
            }

            // 3. Ensure entity sequences

            // 4. Add order
            _orderRepository.Add(order);

            if (request.OrderDraftId is Guid draftReference)
            {
                int? userId = await _currentUser.GetUserIdAsync(cancellationToken);
                await _draftStore.DeleteAsync(draftReference, userId.Value);
                var quotations = await _quotationRepository.GetByDraftReference(
                    draftReference,
                    cancellationToken);

                foreach (Quotation quotation in quotations)
                {
                    quotation.LinkToOrder(order);
                }

                var proformaInvoices = await _proformaInvoiceRepository.GetByDraftReference(
                    draftReference,
                    cancellationToken);

                foreach (ProformaInvoice proformaInvoice in proformaInvoices)
                {
                    proformaInvoice.LinkToOrder(order);
                }
            }

            // 5. Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return order.Id;
        }
        catch (Exception ex)
        {
            return Result<int>.Failure(ex.Message);
        }
    }
}
