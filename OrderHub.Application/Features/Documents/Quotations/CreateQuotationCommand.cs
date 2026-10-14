using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Application.DTOs.Documents;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Documents;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Application.Context;
using OrderHub.Application.Features.OrderDrafts;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Application.Features.Setup.Clients.Contracts;
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
    [RequirePermission(PermissionFeature.Documents, PermissionLevel.Write)]
    public sealed record Command(Guid DraftReference) : IRequest<string>;

    internal class Handler : IRequestHandler<Command, string>
    {
        private readonly IPdfDocumentFactory _documentFactory;
        private readonly ILocalFileAccessService _fileStorageService;
        private readonly IDocumentSequenceRepository _sequenceRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IQuotationRepository _quotationRepository;
        private readonly IDraftReadService _draftReadService;
        private readonly ICurrentUser _currentUser;
        private readonly IClientReader _clientReader;

        public Handler(
            IPdfDocumentFactory documentFactory,
            ILocalFileAccessService fileStorageService,
            IDocumentSequenceRepository sequenceRepository,
            IQuotationRepository quotationRepository,
            IUnitOfWork unitOfWork,
            IDraftReadService draftReadService,
            ICurrentUser currentUser,
            IClientReader clientReader)
        {
            _documentFactory = documentFactory;
            _fileStorageService = fileStorageService;
            _sequenceRepository = sequenceRepository;
            _quotationRepository = quotationRepository;
            _unitOfWork = unitOfWork;
            _draftReadService = draftReadService;
            _currentUser = currentUser;
            _clientReader = clientReader;
        }

        public async Task<string> Handle(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.DraftReference == Guid.Empty)
                throw new ArgumentException("مسودة الطلب غير صالحة.", nameof(request.DraftReference));

            var draft = await _draftReadService.GetOwnedAsync(
                request.DraftReference,
                DraftUser.Require(_currentUser),
                cancellationToken);
            if (draft?.Data is null)
                throw new InvalidOperationException("المسودة غير موجودة أو غير متاحة للمستخدم الحالي.");
            if (draft.Data.ClientId is not > 0)
                throw new InvalidOperationException("يجب اختيار العميل قبل إنشاء عرض السعر.");
            if (draft.Data.Items.Count == 0)
                throw new ArgumentException(
                    "The collection cannot be empty.",
                    nameof(draft.Data.Items));

            var client = await _clientReader.GetClientByIdAsync(draft.Data.ClientId.Value, cancellationToken);
            if (client is null)
                throw new InvalidOperationException("العميل المرتبط بالمسودة غير موجود.");

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
                    currentDate.Month,
                    cancellationToken);

                string documentNumber = $"QT-{currentDate.Year:D4}-{currentDate.Month:D2}-{newSequence:D4}";

                //    - Calculate pricing and item totals.
                var documentPricing = CalculateDocument(draft.Data.Items);

                //    - Create the domain document and add its items.
                quotation = Quotation.Create(
                    documentNumber,
                    currentDate,
                    client.Name,
                    client.PhoneNumber,
                    client.Address,
                    documentPricing.Subtotal,
                    documentPricing.TotalVat,
                    documentPricing.TotalAmount,
                    currentDate.AddDays(30),
                    draft.Id);

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
                await SaveQuotationDocument(quotation);

                // Persist the document together with its reserved number.
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

            return quotation.DocumentNumber;

        }

        private static PricingResult CalculateDocument(IEnumerable<OrderDraftItem> items)
        {
            return PricingCalculator.Calculate(items.Select(item => new PricingItem(
                item.ProductId,
                item.ProductName,
                item.Quantity,
                item.Price,
                item.VAT))
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
            await _fileStorageService.SaveQuotationAsync($"{quotation.DocumentNumber}.pdf", documentBytes);
        }
    }
}
