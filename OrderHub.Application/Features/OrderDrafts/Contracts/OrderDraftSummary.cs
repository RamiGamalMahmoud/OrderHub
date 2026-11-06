using Humanizer;
using System;
using System.Globalization;

namespace OrderHub.Application.Features.OrderDrafts.Contracts;

public record OrderDraftSummary(
    Guid Id,
    string ShortId,
    string ClientName,
    int ItemsCount,
    DateTime UpdatedAt,
    string ProductSummary,
    decimal TotalAmount)
{
    public string UpdatedAtHumanized => UpdatedAtLocal.Humanize(dateToCompareAgainst: DateTime.Now, culture: CultureInfo.GetCultureInfo("ar-EG"));
    public DateTime UpdatedAtLocal => UpdatedAt.ToLocalTime();
}
