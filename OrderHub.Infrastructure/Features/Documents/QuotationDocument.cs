using OrderHub.Application.DTOs.Documents;
using OrderHub.Application.Interfaces.Documents;
using OrderHub.Infrastructure.Features.Documents.Components;
using QuestPDF.Fluent;

namespace OrderHub.Infrastructure.Features.Documents;

internal class QuotationDocument : IPdfDocument
{
    private readonly QuotationPdfData _proformaInvoice;

    public QuotationDocument(QuotationPdfData quotationData)
    {
        _proformaInvoice = quotationData;
    }

    public byte[] Build()
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Configure();
                page.Header().Component(new DocumentHeaderComponent(
                    "عرض سعر",
                    _proformaInvoice.DocumentNumber, 
                    _proformaInvoice.IssueDate,
                    _proformaInvoice.Customer.CustomerName,
                    _proformaInvoice.Customer.CustomerPhone,
                    _proformaInvoice.Customer.CustomerAddress));

                page.Content().Component(new QuotationTableComponent(_proformaInvoice));
                page.Footer().Component(new DocumentFooterComponent());
            });
        })
            .GeneratePdf();
    }
}
