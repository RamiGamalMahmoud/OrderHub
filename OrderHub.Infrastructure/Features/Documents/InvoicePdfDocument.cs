using OrderHub.Application.DTOs.Documents;
using OrderHub.Application.Interfaces.Documents;
using OrderHub.Infrastructure.Features.Documents.Components;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace OrderHub.Infrastructure.Features.Documents;

internal class InvoicePdfDocument : IDocument, IPdfDocument
{
    public readonly InvoicePdfData _data;

    public InvoicePdfDocument(InvoicePdfData data)
    {
        _data = data;
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
                "فاتورة بيع",
                _data.DocumentNumber,
                _data.IssueDate,
                _data.Customer.CustomerName,
                _data.Customer.CustomerPhone,
                _data.Customer.CustomerAddress));

            page.Content()
                .PaddingTop(0f)
                .Component(new InvoiceTableComponent(_data));

            page.Footer().Component(new DocumentFooterComponent());
        });
    }
}
