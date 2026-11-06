using System;

namespace OrderHub.Domain.Common;

public static class HexColor
{
    public const string Default = "#2563EB";

    public static bool TryNormalize(string value, out string normalized)
    {
        normalized = null;

        if (value is null ||
            value.Length != 7 ||
            value[0] != '#')
        {
            return false;
        }

        for (int index = 1; index < value.Length; index++)
        {
            if (!Uri.IsHexDigit(value[index]))
            {
                return false;
            }
        }

        normalized = value.ToUpperInvariant();
        return true;
    }

    public static string Normalize(string value)
    {
        if (!TryNormalize(value, out string normalized))
        {
            throw new ArgumentException("Color must be a six-digit hexadecimal value.", nameof(value));
        }

        return normalized;
    }
}
