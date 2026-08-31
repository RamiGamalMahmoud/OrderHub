using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;

namespace OrderHub.Infrastructure.Features.Documents.Components;

public class DocumentHeaderComponent : IComponent
{
    private readonly string _title;
    private readonly string _invoiceNumber;
    private readonly string _date;

    private readonly string _customerName;
    private readonly string _customerPhone;
    private readonly string _customerAddress;

    public DocumentHeaderComponent(
        string title,
        string invoiceNumber,
        string date,
        string customerName,
        string customerPhone,
        string customerAddress)
    {
        _title = title;
        _invoiceNumber = invoiceNumber;
        _date = date;
        _customerName = customerName;
        _customerPhone = customerPhone;
        _customerAddress = customerAddress;
    }

    public void Compose(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(infoCol =>
                {
                    float padding = 3f;
                    float titleWidth = 40f;
                    infoCol.Item()
                       .PaddingVertical(padding)
                       .Row(row =>
                       {
                           row.ConstantItem(titleWidth)
                               .AlignRight()
                               .Text("اسم العميل:")
                               .Bold()
                               .FontSize(9f);

                           row.RelativeItem()
                               .AlignRight()
                               .Text(_customerName)
                               .FontSize(9f);
                       });

                    // --------------------------------------------------------
                    // Phone
                    // --------------------------------------------------------

                    infoCol.Item()
                        .PaddingVertical(padding)
                        .Row(row =>
                        {
                            row.ConstantItem(titleWidth)
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

                    // --------------------------------------------------------
                    // Address
                    // --------------------------------------------------------

                    infoCol.Item()
                        .PaddingVertical(padding)
                        .Row(row =>
                        {
                            row.ConstantItem(titleWidth)
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

                row.AutoItem()
                    .Column(centerHeaderColumn =>
                    {
                        centerHeaderColumn.Item().AlignCenter()
                        .Column(cl =>
                        {
                            cl.Item()
                                .Width(50)
                                .AlignTop()
                                .Image("./Resources/Images/almasajid.png");

                            cl.Item()
                                .Text("متجر المساجد")
                                .Bold()
                                .FontSize(12f);

                            cl.Item()
                                .Text("\u200E+966-569005020\u200E")
                                .Bold()
                                .FontSize(8.5f)
                                .FontColor(Colors.Grey.Darken2)
                                .DirectionFromLeftToRight();
                        });
                    });

                row.RelativeItem().AlignLeft().Column(metaCol =>
                {
                    metaCol.Item()
                        .Text(_title)
                        .Bold()
                        .Bold()
                        .FontSize(11f);

                    metaCol.Item()
                        .PaddingTop(3f)
                        .Text($"{_invoiceNumber}")
                        .Bold()
                        .FontSize(9f);

                    metaCol.Item()
                        .Text($"التاريخ: {_date}")
                        .FontSize(8.5f)
                        .FontColor(Colors.Grey.Darken2);
                });
            });

            column.Item()
                .PaddingVertical(4f)
                .LineHorizontal(0.8f);
        });
    }
}