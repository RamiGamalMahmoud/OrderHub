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

namespace OrderHub.Application.Features.Documents.Quotations;

public static class CreateQuotationCommand
{
    public record Command(QuotationData Data) : IRequest<string>;

    public record QuotationData(
        Guid SourceDraftReference,
        string CustomerName,
        string CustomerPhone,
        string CustomerAddress,
        decimal Discount,
        IReadOnlyCollection<QuotationItemData> Items);

    public record QuotationItemData(
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
        private readonly IQuotationRepository _quotationRepository;

        public Handler(
            IPdfDocumentFactory documentFactory,
            IFileStorageService fileStorageService,
            IDocumentSequenceRepository sequenceRepository,
            IQuotationRepository quotationRepository,
            IUnitOfWork unitOfWork)
        {
            _documentFactory = documentFactory;
            _fileStorageService = fileStorageService;
            _sequenceRepository = sequenceRepository;
            _quotationRepository = quotationRepository;
            _unitOfWork = unitOfWork;
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
            Quotation quotation;
            try
            {
                DateTime currentDate = DateTime.UtcNow;

                // Generate the document number.
                int newSequence = await _sequenceRepository.ReserveNextNumberAsync(
                    Domain.Enums.DocumentType.Quotation,
                    currentDate.Year,
                    currentDate.Month);

                string documentNumber = $"QT-{currentDate.Year:D4}-{currentDate.Month:D2}-{newSequence:D4}";

                //    - Calculate pricing and item totals.
                var documentPricing = CalculateDocument(request.Data.Items);

                //    - Create the domain document and add its items.
                quotation = Quotation.Create(
                    documentNumber,
                    currentDate,
                    request.Data.CustomerName,
                    request.Data.CustomerPhone,
                    request.Data.CustomerAddress,
                    documentPricing.Subtotal,
                    documentPricing.TotalVat,
                    documentPricing.TotalAmount,
                    currentDate.AddDays(30),
                    request.Data.SourceDraftReference);

                foreach (var item in documentPricing.Items)
                {
                    quotation.AddItem(QuotationItem.Create(
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
                await _quotationRepository.AddAsync(quotation, cancellationToken);

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
            await SaveQuotationDocument(quotation);


            // 5. Store & Return
            //    - Save the PDF file.
            //    - Return the document number.
            return quotation.DocumentNumber;

        }

        private static PricingResult CalculateDocument(IEnumerable<QuotationItemData> items)
        {
            return DocumentPricingCalculator.Calculate(items.Select(item => new PricingItem(
                item.ProductId,
                item.ProductName,
                item.Quantity,
                item.Price,
                item.VatRate))
                .ToList());
        }

        private async Task SaveQuotationDocument(Quotation quotation)
        {
            //    - Map the domain document to its dedicated PdfData.
            QuotationPdfData quotationPdfData = new QuotationPdfData(
                quotation.DocumentNumber,
                quotation.SourceDraftReference,
                null,
                null,
                $"{quotation.IssueDate:dd-MM-yyyy}",
                new DocumentCustomer(
                    quotation.CustomerName,
                    quotation.CustomerPhone,
                    quotation.CustomerAddress),

                quotation.Subtotal,
                quotation.TotalVat,
                quotation.TotalAmount,

                quotation.Items.Select(item => new QuotationPdfDataItem(
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
            IPdfDocument document = _documentFactory.Create(quotationPdfData);

            //    - Build the PDF.
            byte[] documentBytes = document.Build();
            await _fileStorageService.SaveQuotationDocumentAsync(documentBytes, $"{quotation.DocumentNumber}.pdf");
        }
    }
}
