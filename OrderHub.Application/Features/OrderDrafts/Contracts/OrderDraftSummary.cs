using Humanizer;
using System;
using System.Globalization;

namespace OrderHub.Application.Features.OrderDrafts.Contracts;

public record OrderDraftSummary(
    Guid Id,
    string ClientName,
    int ItemsCount,
    DateTime UpdatedAt)
{
    public string UpdatedAtHumanized => UpdatedAtLocal.Humanize(dateToCompareAgainst: DateTime.Now, culture: CultureInfo.GetCultureInfo("ar-EG"));
    public DateTime UpdatedAtLocal => UpdatedAt.ToLocalTime();
}