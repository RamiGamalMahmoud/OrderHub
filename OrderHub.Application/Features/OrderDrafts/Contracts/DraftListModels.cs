using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.OrderDrafts.Contracts;

public enum DraftAgeFilter
{
    All = 0,
    Last24Hours = 1,
    Last7Days = 2,
    OlderThanMonth = 3
}

public enum DraftItemsFilter
{
    All = 0,
    WithItems = 1,
    Empty = 2
}

public enum DraftSort
{
    LatestFirst = 0,
    OldestFirst = 1,
    MostItems = 2
}

public sealed record DraftListRequest(
    int Page = 1,
    int PageSize = 5,
    string SearchTerm = null,
    DraftAgeFilter AgeFilter = DraftAgeFilter.All,
    DraftItemsFilter ItemsFilter = DraftItemsFilter.All,
    DraftSort Sort = DraftSort.LatestFirst);

public sealed record DraftListStats(
    int TotalCount,
    int ActiveLast24Hours,
    int EmptyCount,
    DateTime? OldestUpdatedAt);

public sealed record DraftListPage(
    IReadOnlyList<OrderDraftSummary> Items,
    int TotalCount,
    int Page,
    int PageSize,
    DraftListStats Stats);
