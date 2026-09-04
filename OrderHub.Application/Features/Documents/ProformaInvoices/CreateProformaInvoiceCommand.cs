using MediatR;
using OrderHub.Application.DTOs.Documents;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Documents;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Application.Interfaces.Stores;
using OrderHub.Domain.Models.CommercialDocuments;
using OrderHub.Domain.Services.Pricing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Documents.ProformaInvoices;

public static class CreateProformaInvoiceCommand
{
    public record Command(ProformaInvoiceData Data) : IRequest<string>;
    public record ProformaInvoiceData(
    Guid SourceDraftReference,
    string CustomerName,
    string CustomerPhone,
    string CustomerAddress,
    decimal Discount,
    IReadOnlyCollection<ProformaInvoiceItemData> Items);

    public record ProformaInvoiceItemData(
        int ProductId,
        string ProductName,
        decimal Price,
        decimal Quantity,
        decimal VatRate);

    internal class Handler : IRequestHandler<Command, string>
    {
        private readonly IPdfDocumentFactory _documentFactory;
        private readonly IFileStorageService _fileStorageService;
        private readonly IDocumentSequenceRepository _sequenceRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProformaInvoiceRepository _proformaInvoiceRepository;

        public Handler(
            IPdfDocumentFactory documentFactory,
            IFileStorageService fileStorageService,
            IDocumentSequenceRepository sequenceRepository,
            IUnitOfWork unitOfWork,
            IProformaInvoiceRepository proformaInvoiceRepository)
        {
            _documentFactory = documentFactory;
            _fileStorageService = fileStorageService;
            _sequenceRepository = sequenceRepository;
            _unitOfWork = unitOfWork;
            _proformaInvoiceRepository = proformaInvoiceRepository;
        }

        public async Task<string> Handle(Command request, CancellationToken cancellationToken)
        {
            // 1. Validate & Prepare
            //    - Receive the command.
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Data.CustomerName);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Data.CustomerPhone);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Data.CustomerAddress);
            ArgumentNullException.ThrowIfNull(request.Data.Items);
            if (request.Data.Items.Count == 0)
                throw new ArgumentException(
                    "The collection cannot be empty.",
                    nameof(request.Data.Items));

            // 2. Create Document
            //    - Begin transaction.
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            ProformaInvoice proformaInvoice;
            try
            {
                DateTime currentDate = DateTime.UtcNow;

                // Generate the document number.
                int newSequence = await _sequenceRepository.ReserveNextNumberAsync(
                    Domain.Enums.DocumentType.ProformaInvoice,
                    currentDate.Year,
                    currentDate.Month);

                string documentNumber = $"PI-{currentDate.Year:D4}-{currentDate.Month:D2}-{newSequence:D4}";

                //    - Calculate pricing and item totals.
                var documentPricing = CalculateDocument(request.Data.Items);

                //    - Create the domain document and add its items.
                proformaInvoice = ProformaInvoice.Create(
                    documentNumber,
                    currentDate,
                    request.Data.CustomerName,
                    request.Data.CustomerPhone,
                    request.Data.CustomerAddress,
                    documentPricing.Subtotal,
                    documentPricing.TotalVat,
                    documentPricing.TotalAmount,
                    request.Data.SourceDraftReference);

                foreach (var item in documentPricing.Items)
                {
                    proformaInvoice.AddItem(ProformaInvoiceItem.Create(
                        item.ProductId,
                        item.ProductName,
                        item.Quantity,
                        item.UnitPrice,
                        item.VatRate,
                        item.Subtotal,
                        item.VatAmount,
                        item.Total));
                }
                // 3. Persist
                //    - Add the document to the repository.
                await _proformaInvoiceRepository.AddAsync(proformaInvoice, cancellationToken);

                //    - Save changes.
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                //    - Commit the transaction.
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch (Exception)
            {
                //    - Roll back on failure.
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }

            // 4. Generate PDF
            await SaveProformaInvoiceDocument(proformaInvoice);


            // 5. Store & Return
            //    - Save the PDF file.
            //    - Return the document number.
            return proformaInvoice.DocumentNumber;

        }

        private static PricingResult CalculateDocument(IEnumerable<ProformaInvoiceItemData> items)
        {
            return PricingCalculator.Calculate(items.Select(item => new PricingItem(
                item.ProductId,
                item.ProductName,
                item.Quantity,
                item.Price,
                item.VatRate))
                .ToList());
        }

        private async Task SaveProformaInvoiceDocument(ProformaInvoice proformaInvoice)
        {
            //    - Map the domain document to its dedicated PdfData.
            ProformaInvoicePdfData proformaInvoicePdfData = new ProformaInvoicePdfData(
                proformaInvoice.DocumentNumber,
                proformaInvoice.SourceDraftReference,
                null,
                null,
                $"{proformaInvoice.IssueDate:dd-MM-yyyy}",
                new DocumentCustomer(
                    proformaInvoice.CustomerName,
                    proformaInvoice.CustomerPhone,
                    proformaInvoice.CustomerAddress),

                proformaInvoice.Subtotal,
                proformaInvoice.TotalVat,
                proformaInvoice.TotalAmount,

                proformaInvoice.Items.Select(item => new ProformaInvoicePdfDataDataItem(
                    item.ProductId,
                    item.ProductName,
                    item.Quantity,
                    item.UnitPrice,
                    item.VatRate,
                    item.Subtotal,
                    item.VatAmount,
                    item.Total))
                .ToList());

            //    - Create the appropriate IPdfDocument through the factory.
            IPdfDocument document = _documentFactory.Create(proformaInvoicePdfData);

            //    - Build the PDF.
            byte[] documentBytes = document.Build();
            await _fileStorageService.SaveProformaInvoiceDocumentAsync(documentBytes, $"{proformaInvoice.DocumentNumber}.pdf");
        }
    }
}
