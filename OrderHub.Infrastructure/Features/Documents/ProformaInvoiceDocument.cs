using OrderHub.Application.DTOs.Documents;
using OrderHub.Application.Interfaces.Documents;
using OrderHub.Infrastructure.Features.Documents.Components;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace OrderHub.Infrastructure.Features.Documents;

internal class ProformaInvoiceDocument : IDocument, IPdfDocument
{
    private readonly ProformaInvoicePdfData _proformaInvoice;

    public ProformaInvoiceDocument(ProformaInvoicePdfData proformaInvoice)
    {
        _proformaInvoice = proformaInvoice;
    }

    public byte[] Build()
    {
        return this.GeneratePdf();
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Configure();
            page.Header().Component(new DocumentHeaderComponent(
                "فاتورة أولية",
                _proformaInvoice.DocumentNumber,
                _proformaInvoice.IssueDate,
                _proformaInvoice.Customer.CustomerName,
                _proformaInvoice.Customer.CustomerPhone,
                _proformaInvoice.Customer.CustomerAddress));

            page.Content().Component(new ProformaInvoiceTableComponent(_proformaInvoice));
            page.Footer().Component(new DocumentFooterComponent());
        });
    }
}
