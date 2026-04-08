using OrderHub.Domain.Enums;
using System;

namespace OrderHub.Infrastructure.Helpers;

internal static class OrderEntitySequenceFormatter
{
    private static readonly string[] _arabicMonthNames =
    [
        "يناير",
        "فبراير",
        "مارس",
        "أبريل",
        "مايو",
        "يونيو",
        "يوليو",
        "أغسطس",
        "سبتمبر",
        "أكتوبر",
        "نوفمبر",
        "ديسمبر"
    ];

    public static string Format(RecipientType recipientType, int sequenceNumber, DateTime referenceDate)
    {
        string prefix = recipientType == RecipientType.Deliveryman
            ? "مشوار"
            : "طلب";

        return $"{prefix} {sequenceNumber} {_arabicMonthNames[referenceDate.Month - 1]}";
    }
}
