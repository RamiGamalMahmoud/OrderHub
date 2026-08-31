using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace OrderHub.Infrastructure.Features.Documents.Components;

internal class CustomerInfoComponent : IComponent
{
    private readonly string _customerName;
    private readonly string _customerPhone;
    private readonly string _customerAddress;

    public CustomerInfoComponent(string customerName, string customerPhone, string customerAddress)
    {
        _customerName = customerName;
        _customerPhone = customerPhone;
        _customerAddress = customerAddress;
    }

    public void Compose(IContainer container)
    {
        var borderColor = Colors.Blue.Lighten3;

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
                    .Text("بيانات العميل (المستلم)")
                    .Bold()
                    .FontSize(11f)
                    .FontColor(Colors.Blue.Darken3);

                // --------------------------------------------------------
                // Separator
                // --------------------------------------------------------

                column.Item()
                    .LineHorizontal(0.5f)
                    .LineColor(Colors.Grey.Lighten3);

                // --------------------------------------------------------
                // Name
                // --------------------------------------------------------

                column.Item()
                    .PaddingVertical(7f)
                    .PaddingHorizontal(10f)
                    .Row(row =>
                    {
                        row.ConstantItem(55f)
                            .AlignRight()
                            .Text("الاسم:")
                            .Bold()
                            .FontSize(9f);

                        row.RelativeItem()
                            .AlignRight()
                            .Text(_customerName)
                            .FontSize(9f);
                    });

                column.Item()
                    .LineHorizontal(0.5f)
                    .LineColor(Colors.Grey.Lighten3);

                // --------------------------------------------------------
                // Phone
                // --------------------------------------------------------

                column.Item()
                    .PaddingVertical(7f)
                    .PaddingHorizontal(10f)
                    .Row(row =>
                    {
                        row.ConstantItem(55f)
                            .AlignRight()
                            .Text("الهاتف:")
                            .Bold()
                            .FontSize(9f);

                        row.RelativeItem()
                            .AlignRight()
                            .Text($"\u200E{_customerPhone}\u200E")
                            .FontSize(9f)
                            .DirectionFromLeftToRight();
                    });

                column.Item()
                    .LineHorizontal(0.5f)
                    .LineColor(Colors.Grey.Lighten3);

                // --------------------------------------------------------
                // Address
                // --------------------------------------------------------

                column.Item()
                    .PaddingVertical(7f)
                    .PaddingHorizontal(10f)
                    .Row(row =>
                    {
                        row.ConstantItem(55f)
                            .AlignRight()
                            .Text("العنوان:")
                            .Bold()
                            .FontSize(9f);

                        row.RelativeItem()
                            .AlignRight()
                            .Text(_customerAddress)
                            .FontSize(9f)
                            .DirectionFromRightToLeft();
                    });
            });
    }
}
