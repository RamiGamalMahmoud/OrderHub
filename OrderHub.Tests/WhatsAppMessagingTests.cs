using OrderHub.Application.Features.Orders.GetSummaries;
using System.Linq;

namespace OrderHub.Tests;

public sealed class WhatsAppPhoneTests
{
    [Theory]
    [InlineData("0500112233", "966500112233")]
    [InlineData("+966 50 011 2233", "966500112233")]
    [InlineData("500112233", "966500112233")]
    [InlineData("00966500112233", "966500112233")]
    [InlineData("+44 7700 900123", "447700900123")]
    public void Normalises_numbers_to_wa_me_digits(string raw, string expected) =>
        Assert.Equal(expected, WhatsAppPhone.TryNormalize(raw));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0500112")]
    [InlineData("لا يوجد")]
    public void Rejects_numbers_that_cannot_be_dialled(string? raw) =>
        Assert.Null(WhatsAppPhone.TryNormalize(raw));

    [Fact]
    public void Formats_local_saudi_numbers_readably()
    {
        Assert.Equal("+966 500112233", WhatsAppPhone.Format("0500112233"));
        Assert.Equal("+447700900123", WhatsAppPhone.Format("+44 7700 900123"));
        Assert.Equal("غير متاح", WhatsAppPhone.Format(null));
        Assert.Equal("داخلي 12", WhatsAppPhone.Format("داخلي 12"));
    }

    [Fact]
    public void Direct_url_targets_the_recipient_and_escapes_the_message()
    {
        var url = WhatsAppPhone.BuildDirectUrl("0500112233", "سطر أول\nسطر ثانٍ");

        Assert.NotNull(url);
        Assert.StartsWith("https://wa.me/966500112233?text=", url);
        Assert.Contains("%0A", url);
        Assert.DoesNotContain("سطر", url);
    }

    [Fact]
    public void Direct_url_is_unavailable_without_a_valid_phone()
    {
        Assert.Null(WhatsAppPhone.BuildDirectUrl(null, "نص"));
        Assert.Null(WhatsAppPhone.BuildDirectUrl("123", "نص"));
    }

    [Fact]
    public void Picker_url_omits_the_recipient_so_whatsapp_asks_for_one()
    {
        var url = WhatsAppPhone.BuildPickerUrl("نص");

        Assert.Equal("https://wa.me/?text=%D9%86%D8%B5", url);
    }
}

public sealed class WhatsAppMarkdownTests
{
    [Fact]
    public void Parses_whatsapp_markers_into_typed_segments()
    {
        var segments = WhatsAppMarkdown.Parse("*مرحبا* و_عالم_ و~قديم~\nسطر https://example.com/orders/1 ok");

        Assert.Equal(
            [
                WhatsAppSegmentKind.Bold,
                WhatsAppSegmentKind.Text,
                WhatsAppSegmentKind.Italic,
                WhatsAppSegmentKind.Text,
                WhatsAppSegmentKind.Strike,
                WhatsAppSegmentKind.Text,
                WhatsAppSegmentKind.Link,
                WhatsAppSegmentKind.Text
            ],
            segments.Select(segment => segment.Kind));
        Assert.Equal("مرحبا", segments[0].Text);
        Assert.Equal(" و", segments[1].Text);
        Assert.Equal("عالم", segments[2].Text);
        Assert.Equal("قديم", segments[4].Text);
        Assert.Equal("https://example.com/orders/1", segments[6].Text);
        Assert.Contains("\nسطر ", segments[5].Text);
    }

    [Fact]
    public void Unbalanced_or_spaced_markers_stay_literal()
    {
        var segments = WhatsAppMarkdown.Parse("2 * 3 و *غير مغلق");

        var text = Assert.Single(segments);
        Assert.Equal(WhatsAppSegmentKind.Text, text.Kind);
        Assert.Equal("2 * 3 و *غير مغلق", text.Text);
    }

    [Fact]
    public void Markup_in_stored_text_is_never_treated_as_html()
    {
        var segments = WhatsAppMarkdown.Parse("<b>اسم</b>");

        var text = Assert.Single(segments);
        Assert.Equal(WhatsAppSegmentKind.Text, text.Kind);
        Assert.Equal("<b>اسم</b>", text.Text);
    }

    [Fact]
    public void Empty_messages_produce_no_segments()
    {
        Assert.Empty(WhatsAppMarkdown.Parse(null));
        Assert.Empty(WhatsAppMarkdown.Parse(""));
    }
}
