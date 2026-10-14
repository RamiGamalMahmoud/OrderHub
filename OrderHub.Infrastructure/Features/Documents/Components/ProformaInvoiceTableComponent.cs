using OrderHub.Application.DTOs.Documents;
using OrderHub.Domain.Models.CommercialDocuments;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Linq;

namespace OrderHub.Infrastructure.Features.Documents.Components;

internal class ProformaInvoiceTableComponent : IComponent
{
    private readonly ProformaInvoicePdfData _data;

    public ProformaInvoiceTableComponent(ProformaInvoicePdfData data)
    {
        _data = data;
    }

    public void Compose(IContainer container)
    {
        var totalQuantity = _data.Items.Sum(x => x.Quantity);
        var itemCount = _data.Items.Count;

        container.Column(column =>
        {
            // ============================================================
            // Products Table
            // ============================================================

            column.Item().Table(table =>
            {
                var cellBorderColor = Colors.Grey.Lighten1;
                float cellBorderWidth = 0.8f;

                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(.7f);
                    columns.RelativeColumn(2.8f);
                    columns.RelativeColumn(1.25f);
                    columns.RelativeColumn(.8f);
                    columns.RelativeColumn(.9f);
                    columns.RelativeColumn(1.1f);
                    columns.RelativeColumn(1.3f);
                });

                // --------------------------------------------------------
                // Header
                // --------------------------------------------------------

                table.Header(header =>
                {
                    var headerBackground = Colors.Grey.Lighten2;
                    var headerStyle = TextStyle.Default
                        .Bold()
                        .FontColor(Colors.Grey.Darken3)
                        .FontSize(10f);

                    header.Cell()
                        .Background(headerBackground)
                        .Border(cellBorderWidth)
                        .BorderColor(cellBorderColor)
                        .Padding(6f)
                        .AlignCenter()
                        .Text("متسلسل")
                        .Style(headerStyle);

                    header.Cell()
                        .Background(headerBackground)
                        .Border(cellBorderWidth)
                        .BorderColor(cellBorderColor)
                        .Padding(6f)
                        .AlignRight()
                        .Text("المنتج")
                        .Style(headerStyle);

                    header.Cell()
                        .Background(headerBackground)
                        .Border(cellBorderWidth)
                        .BorderColor(cellBorderColor)
                        .Padding(6f)
                        .AlignRight()
                        .Text("سعر الوحدة قبل الضريبة")
                        .Style(headerStyle);

                    header.Cell()
                        .Background(headerBackground)
                        .Border(cellBorderWidth)
                        .BorderColor(cellBorderColor)
                        .Padding(6f)
                        .AlignRight()
                        .Text("الكمية")
                        .Style(headerStyle);

                    header.Cell()
                        .Background(headerBackground)
                        .Border(cellBorderWidth)
                        .BorderColor(cellBorderColor)
                        .Padding(6f)
                        .AlignRight()
                        .Text("الضريبة %")
                        .Style(headerStyle);

                    header.Cell()
                        .Background(headerBackground)
                        .Border(cellBorderWidth)
                        .BorderColor(cellBorderColor)
                        .Padding(6f)
                        .AlignRight()
                        .Text("قيمة الضريبة")
                        .Style(headerStyle);

                    header.Cell()
                        .Background(headerBackground)
                        .Border(cellBorderWidth)
                        .BorderColor(cellBorderColor)
                        .Padding(6f)
                        .AlignRight()
                        .Text("الإجمالي شامل الضريبة")
                        .Style(headerStyle);
                });

                // --------------------------------------------------------
                // Rows
                // --------------------------------------------------------

                int itemNumber = 1;

                const float paddingSize = 6f;

                foreach (var item in _data.Items)
                {
                    table.Cell()
                        .Border(cellBorderWidth)
                        .BorderColor(cellBorderColor)
                        .Padding(paddingSize)
                        .AlignCenter()
                        .Text($"{itemNumber:D2}");

                    table.Cell()
                        .Border(cellBorderWidth)
                        .BorderColor(cellBorderColor)
                        .Padding(paddingSize)
                        .AlignRight()
                        .Text(item.ProductName)
                        .Bold();

                    table.Cell()
                        .Border(cellBorderWidth)
                        .BorderColor(cellBorderColor)
                        .Padding(paddingSize)
                        .AlignRight()
                        .Text($"{item.UnitPrice:N2}");

                    table.Cell()
                        .Border(cellBorderWidth)
                        .BorderColor(cellBorderColor)
                        .Padding(paddingSize)
                        .AlignRight()
                        .Text($"{item.Quantity:N0}");

                    table.Cell()
                        .Border(cellBorderWidth)
                        .BorderColor(cellBorderColor)
                        .Padding(paddingSize)
                        .AlignRight()
                        .Text($"{item.VatRate:N2}");

                    table.Cell()
                        .Border(cellBorderWidth)
                        .BorderColor(cellBorderColor)
                        .Padding(paddingSize)
                        .AlignRight()
                        .Text($"{item.VatAmount:N2}");

                    table.Cell()
                        .Border(cellBorderWidth)
                        .BorderColor(cellBorderColor)
                        .Padding(paddingSize)
                        .AlignRight()
                        .Text($"{item.Total:N2}");

                    itemNumber++;
                }
            });

            // ============================================================
            // Customer + Document Summary
            // ============================================================

            column.Item()
                .PaddingTop(10f)
                .Component(new DocumentSummaryComponent(
                    itemCount,
                    totalQuantity,
                    _data.Subtotal,
                    0,
                    _data.TotalVat,
                    _data.TotalAmount));
        });
    }
}
