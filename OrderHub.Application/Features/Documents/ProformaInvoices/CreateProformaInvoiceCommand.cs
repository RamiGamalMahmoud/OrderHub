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

namespace OrderHub.Application.Features.Documents.ProformaInvoices;

public static class CreateProformaInvoiceCommand
{
    [RequirePermission(PermissionFeature.Documents, PermissionLevel.Write)]
    public sealed record Command(Guid DraftReference) : IRequest<string>;

    internal class Handler : IRequestHandler<Command, string>
    {
        private readonly IPdfDocumentFactory _documentFactory;
        private readonly ILocalFileAccessService _fileStorageService;
        private readonly IDocumentSequenceRepository _sequenceRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProformaInvoiceRepository _proformaInvoiceRepository;
        private readonly IDraftReadService _draftReadService;
        private readonly ICurrentUser _currentUser;
        private readonly IClientReader _clientReader;

        public Handler(
            IPdfDocumentFactory documentFactory,
            ILocalFileAccessService fileStorageService,
            IDocumentSequenceRepository sequenceRepository,
            IUnitOfWork unitOfWork,
            IProformaInvoiceRepository proformaInvoiceRepository,
            IDraftReadService draftReadService,
            ICurrentUser currentUser,
            IClientReader clientReader)
        {
            _documentFactory = documentFactory;
            _fileStorageService = fileStorageService;
            _sequenceRepository = sequenceRepository;
            _unitOfWork = unitOfWork;
            _proformaInvoiceRepository = proformaInvoiceRepository;
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
                throw new InvalidOperationException("يجب اختيار العميل قبل إنشاء الفاتورة الأولية.");
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
            ProformaInvoice proformaInvoice;
            try
            {
                DateTime currentDate = DateTime.UtcNow;

                // Generate the document number.
                int newSequence = await _sequenceRepository.ReserveNextNumberAsync(
                    Domain.Enums.DocumentType.ProformaInvoice,
                    currentDate.Year,
                    currentDate.Month,
                    cancellationToken);

                string documentNumber = $"PI-{currentDate.Year:D4}-{currentDate.Month:D2}-{newSequence:D4}";

                //    - Calculate pricing and item totals.
                var documentPricing = CalculateDocument(draft.Data.Items);

                //    - Create the domain document and add its items.
                proformaInvoice = ProformaInvoice.Create(
                    documentNumber,
                    currentDate,
                    client.Name,
                    client.PhoneNumber,
                    client.Address,
                    documentPricing.Subtotal,
                    documentPricing.TotalVat,
                    documentPricing.TotalAmount,
                    draft.Id);

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
                await SaveProformaInvoiceDocument(proformaInvoice);

                // Persist the document together with its reserved number.
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

            return proformaInvoice.DocumentNumber;

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
            await _fileStorageService.SaveProformaInvoiceAsync(
                $"{proformaInvoice.DocumentNumber}.pdf",
                documentBytes);
        }
    }
}
