using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.OrderDrafts.Contracts;

namespace OrderHub.Application.Features.OrderDrafts.List;

[RequirePermission(PermissionFeature.Orders, PermissionLevel.Read)]
public sealed record GetDraftListQuery(DraftListRequest Request) : IRequest<DraftListPage>;

internal sealed class GetDraftListQueryHandler(ICurrentUser currentUser, IDraftReadService reads, TimeProvider clock)
    : IRequestHandler<GetDraftListQuery, DraftListPage>
{
    public async Task<DraftListPage> Handle(GetDraftListQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<OrderDraftSummary> drafts =
            await reads.GetUserDraftsAsync(DraftUser.Require(currentUser), cancellationToken);

        DateTime now = clock.GetUtcNow().UtcDateTime;
        var stats = new DraftListStats(
            drafts.Count,
            drafts.Count(d => d.UpdatedAt >= now.AddHours(-24)),
            drafts.Count(d => d.ItemsCount == 0),
            drafts.Count == 0 ? null : drafts.Min(d => d.UpdatedAt));

        IEnumerable<OrderDraftSummary> filtered = drafts;

        string term = request.Request.SearchTerm?.Trim().TrimStart('#');
        if (!string.IsNullOrWhiteSpace(term))
        {
            filtered = filtered.Where(d =>
                d.ClientName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                d.ShortId.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        filtered = request.Request.AgeFilter switch
        {
            DraftAgeFilter.Last24Hours => filtered.Where(d => d.UpdatedAt >= now.AddHours(-24)),
            DraftAgeFilter.Last7Days => filtered.Where(d => d.UpdatedAt >= now.AddDays(-7)),
            DraftAgeFilter.OlderThanMonth => filtered.Where(d => d.UpdatedAt < now.AddDays(-30)),
            _ => filtered
        };

        filtered = request.Request.ItemsFilter switch
        {
            DraftItemsFilter.WithItems => filtered.Where(d => d.ItemsCount > 0),
            DraftItemsFilter.Empty => filtered.Where(d => d.ItemsCount == 0),
            _ => filtered
        };

        filtered = request.Request.Sort switch
        {
            DraftSort.OldestFirst => filtered.OrderBy(d => d.UpdatedAt),
            DraftSort.MostItems => filtered.OrderByDescending(d => d.ItemsCount).ThenByDescending(d => d.UpdatedAt),
            _ => filtered.OrderByDescending(d => d.UpdatedAt)
        };

        var filteredList = filtered.ToList();
        int pageSize = Math.Clamp(request.Request.PageSize, 1, 50);
        int totalPages = Math.Max(1, (int)Math.Ceiling(filteredList.Count / (double)pageSize));
        int page = Math.Clamp(request.Request.Page, 1, totalPages);
        var items = filteredList.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new DraftListPage(items, filteredList.Count, page, pageSize, stats);
    }
}
