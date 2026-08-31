using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace OrderHub.Infrastructure.Features.Documents.Components;

public class DocumentFooterComponent : IComponent
{
    public void Compose(IContainer container)
    {
        container.Column(column =>
        {
            column.Item()
                .LineHorizontal(0.8f);

            column.Item()
                .PaddingTop(8f)
                .Row(row =>
                {
                    row.RelativeItem()
                        .Text("شكراً لاختياركم متجر المساجد - يسعدنا خدمتكم")
                        .FontSize(8f)
                        .FontColor(Colors.Grey.Darken1);

                    row.RelativeItem().AlignLeft().Text(x =>
                    {
                        x.Span("صفحة ").FontSize(8f);
                        x.CurrentPageNumber().FontSize(8f).Bold();
                        x.Span(" من ").FontSize(8f);
                        x.TotalPages().FontSize(8f).Bold();
                    });
                });
        });
    }
}
