using OrderHub.Application.DTOs.Documents;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Linq;

namespace OrderHub.Infrastructure.Features.Documents.Components
{
    internal class QuotationTableComponent : IComponent
    {
        private readonly QuotationPdfData _data;

        public QuotationTableComponent(QuotationPdfData quotationItemData)
        {
            _data = quotationItemData;
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
                        columns.RelativeColumn(1.0f); // متسلسل
                        columns.RelativeColumn(3.5f); // المنتج
                        columns.RelativeColumn(1.5f); // سعر الوحدة
                        columns.RelativeColumn(1.0f); // الكمية
                        columns.RelativeColumn(1.5f); // الإجمالي
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
                            .Text("سعر الوحدة")
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
                            .Text("الإجمالي")
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
                            .BorderBottom(0.8f)
                            .Border(cellBorderWidth)
                            .BorderColor(cellBorderColor)
                            .Padding(paddingSize)
                            .AlignCenter()
                            .Text($"{itemNumber:D2}");

                        table.Cell()
                            .BorderBottom(0.8f)
                            .Border(cellBorderWidth)
                            .BorderColor(cellBorderColor)
                            .Padding(paddingSize)
                            .AlignRight()
                            .Text(item.ProductName)
                            .Bold();

                        table.Cell()
                            .BorderBottom(0.8f)
                            .Border(cellBorderWidth)
                            .BorderColor(cellBorderColor)
                            .Padding(paddingSize)
                            .AlignRight()
                            .Text($"{item.UnitPrice:N2}");

                        table.Cell()
                            .BorderBottom(0.8f)
                            .Border(cellBorderWidth)
                            .BorderColor(cellBorderColor)
                            .Padding(paddingSize)
                            .AlignRight()
                            .Text($"{item.Quantity:N0}");

                        table.Cell()
                            .BorderBottom(0.8f)
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
                    _data.TotalVat,
                    0,
                    _data.TotalAmount));
            });
        }
    }
}

