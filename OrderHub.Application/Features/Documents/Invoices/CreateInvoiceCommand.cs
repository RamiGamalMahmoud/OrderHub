using MediatR;
using OrderHub.Application.DTOs.Documents;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Documents;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Application.Interfaces.Stores;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.Models.CommercialDocuments;
using OrderHub.Domain.Services.Pricing;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Documents.Invoices;

public static class CreateInvoiceCommand
{
    public record Command(int OrderId) : IRequest<string>;

    internal sealed class Handler : IRequestHandler<Command, string>
    {
        private readonly IDocumentSequenceRepository _documentSequenceRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPdfDocumentFactory _documentFactory;
        private readonly IFileStorageService _fileStorageService;
        private readonly IOrderRepository _orderRepository;

        public Handler(
            IDocumentSequenceRepository documentSequenceRepository,
            IInvoiceRepository invoiceRepository,
            IUnitOfWork unitOfWork,
            IPdfDocumentFactory documentFactory,
            IFileStorageService fileStorageService,
            IOrderRepository orderRepository)
        {
            _documentSequenceRepository = documentSequenceRepository;
            _invoiceRepository = invoiceRepository;
            _unitOfWork = unitOfWork;
            _documentFactory = documentFactory;
            _fileStorageService = fileStorageService;
            _orderRepository = orderRepository;
        }

        public async Task<string> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            Invoice existingInvoice = await _invoiceRepository.GetByOrderId(request.OrderId, cancellationToken);
            if (existingInvoice is not null)
            {
                await CreateInoiveDocument(existingInvoice, existingInvoice.Order.OrderNumber);
                return existingInvoice.DocumentNumber;
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            Order order = await _orderRepository.GetById(request.OrderId);
            Invoice createdInvoice;
            try
            {

                int year = DateTime.UtcNow.Year;
                int month = DateTime.UtcNow.Month;

                int invoiceNumber =
                    await _documentSequenceRepository.ReserveNextNumberAsync(
                        DocumentType.Invoice,
                        year,
                        month,
                        cancellationToken);

                string invoiceFullNumber =
                    $"INV-{year}-{month:D2}-{invoiceNumber:D4}";

                var invoicePricing = DocumentPricingCalculator.Calculate(order.OrderItems.Select(item => new PricingItem(
                    item.ProductId,
                    item.ProductName,
                    item.Quantity,
                    item.UnitPrice.Value,
                    0.0m))
                    .ToList());

                var itemsResult = order.OrderItems.Select(item => DocumentPricingCalculator.CalculateItem(new PricingItem(
                    item.ProductId,
                    item.ProductName,
                    item.Quantity,
                    item.UnitPrice.Value,
                    0.0m)));

                createdInvoice = Invoice.Create(
                    invoiceFullNumber,
                    DateTime.UtcNow,
                    order.Client.Name.Value,
                    order.Client.Phone.Number.FullNumber,
                    order.Client.Address.FullAddress,
                    invoicePricing.Subtotal,
                    invoicePricing.TotalVat,
                    invoicePricing.TotalAmount,
                    order.Id);

                foreach (var item in itemsResult)
                {
                    InvoiceItem invoiceItem = new InvoiceItem(
                        item.ProductId,
                        item.ProductName,
                        item.Quantity,
                        item.UnitPrice,
                        item.VatRate,
                        item.Subtotal,
                        item.VatAmount,
                        item.Total);
                    createdInvoice.AddItem(invoiceItem);
                }

                await _invoiceRepository.AddAsync(createdInvoice, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }

            await CreateInoiveDocument(createdInvoice, order.OrderNumber);

            return createdInvoice.DocumentNumber;
        }

        private async Task CreateInoiveDocument(Invoice createdInvoice, string orderNumber)
        {
            IPdfDocument document =
                _documentFactory.Create<InvoicePdfData>(
                    new InvoicePdfData(
                        createdInvoice.DocumentNumber,
                        createdInvoice.OrderId,
                        orderNumber,
                        $"{createdInvoice.IssueDate:dd-MM-yyyy}",

                        new DocumentCustomer(
                            createdInvoice.CustomerName,
                            createdInvoice.CustomerPhone,
                            createdInvoice.CustomerAddress),

                        createdInvoice.Subtotal,
                        createdInvoice.TotalVat,
                        createdInvoice.TotalAmount,

                        createdInvoice.Items
                            .Select(item => new InvoicePdfDataItem(
                                item.ProductId,
                                item.ProductName,
                                item.Quantity,
                                item.UnitPrice,
                                item.VatRate,
                                item.Subtotal,
                                item.VatAmount,
                                item.Total))
                            .ToList()));

            byte[] pdfBytes = document.Build();

            await _fileStorageService.SaveInvoiceDocumentAsync(
                pdfBytes,
                $"{createdInvoice.DocumentNumber}.pdf");
        }
    }
}