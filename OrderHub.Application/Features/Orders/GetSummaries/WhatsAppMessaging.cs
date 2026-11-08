#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OrderHub.Application.Features.Orders.GetSummaries;

/// <summary>
/// Normalises a stored recipient phone into the digits WhatsApp links need.
/// Local Saudi numbers (05xxxxxxxx) are expanded to the 9665xxxxxxxx form;
/// international numbers are kept as-is once their separators are removed.
/// </summary>
public static class WhatsAppPhone
{
    public static string? TryNormalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return null;

        if (digits.StartsWith("00", StringComparison.Ordinal)) digits = digits[2..];
        else if (digits.Length == 10 && digits[0] == '0') digits = digits[1..];

        if (digits.Length == 9 && digits[0] == '5') digits = "966" + digits;

        return digits.Length is >= 10 and <= 15 ? digits : null;
    }

    /// <summary>Readable form for the dispatch card; the stored value is kept when it is not a usable number.</summary>
    public static string Format(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "غير متاح";

        var digits = TryNormalize(raw);
        if (digits is null) return raw;
        return digits.StartsWith("966", StringComparison.Ordinal) && digits.Length == 12
            ? $"+966 {digits[3..]}"
            : "+" + digits;
    }

    public static string? BuildDirectUrl(string? raw, string text)
    {
        var digits = TryNormalize(raw);
        return digits is null ? null : $"https://wa.me/{digits}?text={Uri.EscapeDataString(text)}";
    }

    /// <summary>WhatsApp opens without a number, so the user picks the recipient themselves.</summary>
    public static string BuildPickerUrl(string text) =>
        $"https://wa.me/?text={Uri.EscapeDataString(text)}";
}

public enum WhatsAppSegmentKind
{
    Text,
    Bold,
    Italic,
    Strike,
    Link
}

public sealed record WhatsAppSegment(string Text, WhatsAppSegmentKind Kind);

/// <summary>
/// Splits WhatsApp-flavoured markdown into typed segments so the preview renders
/// real elements without ever injecting HTML built from database values.
/// </summary>
public static class WhatsAppMarkdown
{
    public static IReadOnlyList<WhatsAppSegment> Parse(string? text)
    {
        var segments = new List<WhatsAppSegment>();
        if (string.IsNullOrEmpty(text)) return segments;

        var plain = new StringBuilder();

        void FlushPlain()
        {
            if (plain.Length == 0) return;
            segments.Add(new WhatsAppSegment(plain.ToString(), WhatsAppSegmentKind.Text));
            plain.Clear();
        }

        var index = 0;
        while (index < text.Length)
        {
            var current = text[index];

            if (IsUrlStart(text, index))
            {
                FlushPlain();
                var start = index;
                while (index < text.Length && !char.IsWhiteSpace(text[index])) index++;
                segments.Add(new WhatsAppSegment(text[start..index], WhatsAppSegmentKind.Link));
                continue;
            }

            if (current is '*' or '_' or '~')
            {
                var close = text.IndexOf(current, index + 1);
                if (close > index + 1 &&
                    !char.IsWhiteSpace(text[index + 1]) &&
                    !char.IsWhiteSpace(text[close - 1]))
                {
                    FlushPlain();
                    segments.Add(new WhatsAppSegment(text[(index + 1)..close], current switch
                    {
                        '*' => WhatsAppSegmentKind.Bold,
                        '_' => WhatsAppSegmentKind.Italic,
                        _ => WhatsAppSegmentKind.Strike
                    }));
                    index = close + 1;
                    continue;
                }
            }

            plain.Append(current);
            index++;
        }

        FlushPlain();
        return segments;
    }

    private static bool IsUrlStart(string text, int index) =>
        text.AsSpan(index).StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        text.AsSpan(index).StartsWith("https://", StringComparison.OrdinalIgnoreCase);
}
