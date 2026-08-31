using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace OrderHub.Infrastructure.Features.Documents.Components;

internal class DocumentSummaryComponent : IComponent
{

    private readonly int _itemCount;
    private readonly decimal _totalQuantity;
    private readonly decimal _subtotal;
    private readonly decimal _discount;
    private readonly decimal _tax;
    private readonly decimal _total;

    public DocumentSummaryComponent(
        int itemCount,
        decimal totalQuantity,
        decimal subtotal,
        decimal discount,
        decimal tax,
        decimal total)
    {
        _itemCount = itemCount;
        _totalQuantity = totalQuantity;
        _subtotal = subtotal;
        _discount = discount;
        _tax = tax;
        _total = total;
    }

    public void Compose(IContainer container)
    {
        var borderColor = Colors.Grey.Lighten3;

        container
            .Border(0.8f)
            .BorderColor(borderColor)
            .CornerRadius(5f)
            .Column(column =>
            {
                // --------------------------------------------------------
                // Title
                // --------------------------------------------------------

                column.Item()
                    .Background(Colors.Grey.Lighten5)
                    .PaddingVertical(7f)
                    .PaddingHorizontal(10f)
                    .AlignCenter()
                    .Text("ملخص الفاتورة")
                    .Bold()
                    .FontSize(11f)
                    .FontColor(Colors.Blue.Darken3);

                column.Item()
                    .LineHorizontal(0.5f)
                    .LineColor(Colors.Grey.Lighten3);

                // --------------------------------------------------------
                // Summary Rows
                // --------------------------------------------------------

                ComposeSummaryRow(
                    column,
                    "عدد الأصناف",
                    $"{_itemCount:N}");

                ComposeSummaryRow(
                    column,
                    "إجمالي الكمية",
                    $"{_totalQuantity:N}");

                //ComposeSummaryRow(
                //    column,
                //    "الإجمالي قبل الخصم",
                //    $"{_subtotal:N2}");

                //ComposeSummaryRow(
                //    column,
                //    "الخصم",
                //    $"{_discount:N2}");

                //ComposeSummaryRow(
                //    column,
                //    "الضريبة (0%)",
                //    $"{_tax:N2}");

                // --------------------------------------------------------
                // Separator Line
                // --------------------------------------------------------

                column
                    .Item()
                    .PaddingHorizontal(10f)
                    .PaddingVertical(3f)
                    .Height(.5f)
                    .Background(Colors.Blue.Lighten3);

                // --------------------------------------------------------
                // Final Total
                // --------------------------------------------------------

                column.Item()
                    .PaddingVertical(10f)
                    .PaddingHorizontal(10f)
                    .Row(row =>
                    {
                        row.RelativeItem()
                            .AlignRight()
                            .AlignMiddle()
                            .Text("الإجمالي المستحق")
                            .Bold()
                            .FontSize(11f)
                            .FontColor(Colors.Blue.Darken3);

                        row.RelativeItem()
                            .AlignLeft()
                            .AlignMiddle()
                            .Padding(5f)
                            .Text($"{_total:N2} ر.س")
                            .BackgroundColor(Colors.Blue.Lighten5)
                            .Bold()
                            .FontSize(11f)
                            .FontColor(Colors.Blue.Darken3);
                    });
            });
    }

    // ====================================================================
    // Summary Row Helper
    // ====================================================================
    private static void ComposeSummaryRow(ColumnDescriptor column, string label, string value)
    {
        column.Item()
            .PaddingVertical(4f)
            .PaddingHorizontal(10f)
            .Row(row =>
            {
                row.RelativeItem()
                    .AlignRight()
                    .Text(label)
                    .FontSize(9f)
                    .FontColor(Colors.Grey.Darken4);

                row.AutoItem()
                    .AlignLeft()
                    .Text(value)
                    .Bold()
                    .FontSize(9.5f);
            });
    }
}
