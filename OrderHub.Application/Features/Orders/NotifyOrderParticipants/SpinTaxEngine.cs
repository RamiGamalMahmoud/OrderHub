using System;
using System.Text.RegularExpressions;

namespace OrderHub.Application.Features.Orders.NotifyOrderParticipants;

public static class SpinTaxEngine
{
    private static readonly Random _random = new();

    // Regex يبحث عن أي نص يقع بين أقواس متعرجة { } ولا يحتوي على أقواس متعرجة أخرى بداخله
    private static readonly Regex _spinTaxRegex = new(@"\{([^{}]+)\}", RegexOptions.Compiled);

    public static string Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        // استمرار المعالجة طالما توجد أقواس متعرجة (يدعم الـ Nested SpinTax إذا وُجدت)
        while (_spinTaxRegex.IsMatch(text))
        {
            text = _spinTaxRegex.Replace(text, match =>
            {
                // فصل الخيارات بناءً على علامة الخط الرأسي |
                string[] choices = match.Groups[1].Value.Split('|');

                // اختيار عنصر عشوائي من المصفوفة
                int randomIndex = _random.Next(choices.Length);
                return choices[randomIndex].Trim();
            });
        }

        return text;
    }
}
