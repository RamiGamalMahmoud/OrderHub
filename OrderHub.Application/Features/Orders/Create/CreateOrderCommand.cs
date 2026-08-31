using MediatR;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Application.Interfaces.Stores;
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

public static class CreateOrderCommand
{
    public record Command(OrderInput Order) : IRequest<Result<int>>;

    internal class Handler : IRequestHandler<Command, Result<int>>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderEntitySequenceService _orderEntitySequenceService;
        private readonly IQuotationRepository _quotationRepository;
        private readonly IProformaInvoiceRepository _proformaInvoiceRepository;
        private readonly IUnitOfWork _unitOfWork;

        public Handler(
            IOrderRepository orderRepository,
            IOrderEntitySequenceService orderEntitySequenceService,
            IUnitOfWork unitOfWork,
            IQuotationRepository quotationRepository,
            IProformaInvoiceRepository proformaInvoiceRepository)
        {
            _orderRepository = orderRepository;
            _orderEntitySequenceService = orderEntitySequenceService;
            _unitOfWork = unitOfWork;
            _quotationRepository = quotationRepository;
            _proformaInvoiceRepository = proformaInvoiceRepository;
        }

        public async Task<Result<int>> Handle(
    Command request,
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
                    request.Order.DeliveryMethod,
                    request.Order.DeliveryManId,
                    request.Order.ShippingCarrierId,
                    request.Order.PaymentMothodId);

                foreach (Item item in request.Order.OrderItems)
                {
                    Result<OrderItem> result = order.AddOrderItem(
                        item.ProductId,
                        item.ProductName,
                        item.UnitPrice,
                        item.Quantity,
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

                foreach (DeliveryStep step in
                    request.Order.DeliverySteps ?? Enumerable.Empty<DeliveryStep>())
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
                await _orderEntitySequenceService.EnsureEntitySequencesAsync(
                    order,
                    cancellationToken);

                // 4. Add order
                _orderRepository.Add(order);

                Guid draftReference = request.Order.DraftReference.Value;

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

    public record OrderInput(
    int ClientId,
    DeliveryMethod DeliveryMethod,
    int? DeliveryManId,
    int? ShippingCarrierId,
    IEnumerable<Item> OrderItems,
    IEnumerable<DeliveryStep> DeliverySteps,
    int? PaymentMothodId,
    Guid? DraftReference);

    public record DeliveryStep(
        int StepOrder,
        DeliveryMethod DeliveryMethod,
        int HandlerId);

    public record Item(
        int ProductId,
        string ProductName,
        int Quantity,
        decimal UnitPrice,
        string SupplierName,
        int? SupplierId,
        IEnumerable<Property> Properties);

    public record Property(
        int PropertyId,
        string Value);
}