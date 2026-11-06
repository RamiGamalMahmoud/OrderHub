using System.Globalization;
using Humanizer;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Application.Features.OrderDrafts.Delete;
using OrderHub.Application.Features.OrderDrafts.List;
using OrderHub.Application.Interfaces;

namespace OrderHub.Web.Components.Pages.Features.Orders.Drafts;

public partial class Drafts
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    [Inject] private IRequestExecutor RequestExecutor { get; set; } = null!;

    [Inject] private IDialogService DialogService { get; set; } = null!;

    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    [Inject] private ILogger<Drafts> Logger { get; set; } = null!;

    private static readonly (DraftAgeFilter Value, string Label)[] _ageOptions =
    [
        (DraftAgeFilter.All, "الكل"),
        (DraftAgeFilter.Last24Hours, "آخر 24 ساعة"),
        (DraftAgeFilter.Last7Days, "آخر 7 أيام"),
        (DraftAgeFilter.OlderThanMonth, "أقدم من شهر")
    ];

    private static readonly (DraftItemsFilter Value, string Label)[] _itemsOptions =
    [
        (DraftItemsFilter.All, "جميع الحالات"),
        (DraftItemsFilter.WithItems, "تحتوي منتجات (> 0)"),
        (DraftItemsFilter.Empty, "مسودات فارغة (0)")
    ];

    private static readonly (DraftSort Value, string Label)[] _sortOptions =
    [
        (DraftSort.LatestFirst, "الأحدث أولاً"),
        (DraftSort.OldestFirst, "الأقدم أولاً"),
        (DraftSort.MostItems, "الأعلى أصنافاً")
    ];

    private IReadOnlyList<OrderDraftSummary> _items = [];
    private DraftListStats? _stats;
    private int _page = 1;
    private int _pageSize = 8;
    private int _filteredTotal;
    private string? _searchTerm;
    private DraftAgeFilter _ageFilter = DraftAgeFilter.All;
    private DraftItemsFilter _itemsFilter = DraftItemsFilter.All;
    private DraftSort _sort = DraftSort.LatestFirst;
    private bool _busy;
    private bool _refreshPending;
    private string? _error;
    private long _loadVersion;

    private bool HasFilters =>
        !string.IsNullOrWhiteSpace(_searchTerm) ||
        _ageFilter != DraftAgeFilter.All ||
        _itemsFilter != DraftItemsFilter.All;

    private string OldestAgeLabel
    {
        get
        {
            if (_stats?.OldestUpdatedAt is not { } oldest)
                return "—";

            DateTime local = DateTime.SpecifyKind(oldest, DateTimeKind.Utc).ToLocalTime();
            return local.Humanize(
                dateToCompareAgainst: DateTime.Now,
                culture: CultureInfo.GetCultureInfo("ar-EG"));
        }
    }

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync(bool resetPage = false)
    {
        if (resetPage)
            _page = 1;

        long version = ++_loadVersion;
        _busy = true;
        _error = null;

        try
        {
            var result = await RequestExecutor.ExecuteAsync(new GetDraftListQuery(new DraftListRequest(
                Page: _page,
                PageSize: _pageSize,
                SearchTerm: _searchTerm,
                AgeFilter: _ageFilter,
                ItemsFilter: _itemsFilter,
                Sort: _sort)));

            if (version != _loadVersion)
                return;

            _items = result.Items;
            _filteredTotal = result.TotalCount;
            _stats = result.Stats;
            _page = result.Page;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to load order drafts.");
            if (version == _loadVersion)
                _error = "تعذر تحميل المسودات. حاول مرة أخرى.";
        }
        finally
        {
            if (version == _loadVersion)
                _busy = false;
        }
    }

    private async Task RefreshAsync()
    {
        if (_busy)
        {
            _refreshPending = true;
            return;
        }

        do
        {
            _refreshPending = false;
            await LoadAsync();
        } while (_refreshPending);
    }

    private async Task OnSearchChanged()
    {
        await LoadAsync(resetPage: true);
    }

    private async Task OnAgeChanged(ChangeEventArgs args)
    {
        if (!Enum.TryParse(args.Value?.ToString(), out DraftAgeFilter value) || value == _ageFilter)
            return;

        _ageFilter = value;
        await LoadAsync(resetPage: true);
    }

    private async Task OnItemsChanged(ChangeEventArgs args)
    {
        if (!Enum.TryParse(args.Value?.ToString(), out DraftItemsFilter value) || value == _itemsFilter)
            return;

        _itemsFilter = value;
        await LoadAsync(resetPage: true);
    }

    private async Task OnSortChanged(ChangeEventArgs args)
    {
        if (!Enum.TryParse(args.Value?.ToString(), out DraftSort value) || value == _sort)
            return;

        _sort = value;
        await LoadAsync(resetPage: true);
    }

    private async Task OnPageChanged(int page)
    {
        _page = Math.Max(1, page);
        await LoadAsync();
    }

    private void NavigateToDraft(Guid draftId) => Navigation.NavigateTo($"/orders/continue/{draftId}");

    private async Task DeleteDraftAsync(Guid draftId)
    {
        if (_busy)
            return;

        bool? confirmed = await DialogService.ShowMessageBoxAsync(
            "حذف مسودة", "هل تريد حذف هذه المسودة؟", yesText: "نعم", cancelText: "لا");
        if (confirmed is not true)
            return;

        try
        {
            var result = await RequestExecutor.ExecuteAsync(new DeleteDraftCommand(draftId));
            if (!result.IsSuccess)
            {
                Snackbar.Add(result.ErrorMessage, Severity.Error);
                return;
            }

            Snackbar.Add("تم حذف المسودة.", Severity.Success);
            await LoadAsync();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to delete draft {DraftId}.", draftId);
            Snackbar.Add("تعذر حذف المسودة. حاول مرة أخرى.", Severity.Error);
        }
    }

    private async Task DeleteOldAsync()
    {
        if (_busy)
            return;

        bool? confirmed = await DialogService.ShowMessageBoxAsync(
            "حذف المسودات القديمة",
            "سيتم حذف جميع مسوداتك الأقدم من 30 يوماً. لا يمكن التراجع عن هذا الإجراء.",
            yesText: "حذف",
            cancelText: "إلغاء");
        if (confirmed is not true)
            return;

        try
        {
            var result = await RequestExecutor.ExecuteAsync(new DeleteOldDraftsCommand(30));
            if (!result.IsSuccess)
            {
                Snackbar.Add(result.ErrorMessage, Severity.Error);
                return;
            }

            Snackbar.Add(
                result.Value == 0
                    ? "لا توجد مسودات أقدم من 30 يوماً."
                    : $"تم حذف {result.Value.ToString(CultureInfo.InvariantCulture)} مسودة.",
                Severity.Success);

            await LoadAsync(resetPage: true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to delete old drafts.");
            Snackbar.Add("تعذر حذف المسودات القديمة. حاول مرة أخرى.", Severity.Error);
        }
    }
}
