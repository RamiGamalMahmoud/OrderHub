using OrderHub.Application.DTOs.Documents;
using OrderHub.Application.Interfaces.Documents;

namespace OrderHub.Infrastructure.Features.Documents;

internal class PdfDocumentFactory : IPdfDocumentFactory
{
    public IPdfDocument Create<TData>(TData data) where TData : class
    {
        return data switch
        {
            InvoicePdfData invoiceData => new InvoicePdfDocument(invoiceData),

            QuotationPdfData quotationData => new QuotationDocument(quotationData),

            ProformaInvoicePdfData proformaInvoicePdf => new ProformaInvoiceDocument(proformaInvoicePdf),

            _ => throw new System.NotSupportedException($"Document type {typeof(TData).Name}, is not supported in the system for now.")
        };
    }
}
