using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace OrderHub.Infrastructure.Features.Documents;

internal static class PageDescriptorExtensions
{
    public static PageDescriptor Configure(this PageDescriptor page)
    {
        page.Size(PageSizes.A5);
        page.Margin(15f);
        page.PageColor(Colors.White);

        page
            .Background()
            .AlignCenter()
            .AlignMiddle()
            .Image(@"./Resources/Images/documents-background.png");

        page.ContentFromRightToLeft();

        page.DefaultTextStyle(x => x
            .FontFamily("Arial")
            .FontSize(9.5f)
            .FontColor(Colors.Grey.Darken4)
        );
        return page;
    }
}
