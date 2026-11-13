using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Presentation;
using OrderHub.Application.Features.Inventory.WarehouseStock;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.Web.Components.Common;
using OrderHub.Web.Services;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Web.Components.Pages.Features.Inventory.Balances;

public partial class WarehouseDetails : IDisposable
{
    private static readonly PageScaffold.CrumbItem[] _defaultCrumbs =
    [
        new("المخزون", "/inventory"),
        new("تفاصيل المخزن")
    ];

    private MudTable<WarehouseStockRow>? _table;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _searchDelay;
    private CancellationTokenSource? _loadCancellation;
    private TableData<WarehouseStockRow> _latestData = new() { Items = [], TotalItems = 0 };
    private PageScaffold.CrumbItem[] _crumbs = _defaultCrumbs;
    private WarehouseStockResult? _stock;
    private string? _search;
    private string? _activeSearch;
    private string? _error;
    private int? _categoryId;
    private StockStatusFilter _status = StockStatusFilter.All;
    private bool _hideZero;
    private StockSortColumn _sort = StockSortColumn.Available;
    private bool _descending = true;
    private int _pageIndex = 1;
    private int _pageSize = 10;
    private int _totalCount;
    private int _loadedWarehouseId;
    private long _requestVersion;
    private bool _loading = true;
    private bool _disposed;

    [Parameter]
    public int WarehouseId { get; set; }

    private bool HasFilters =>
        !string.IsNullOrWhiteSpace(_activeSearch) ||
        _categoryId.HasValue ||
        _status != StockStatusFilter.All ||
        _hideZero;

    protected override async Task OnParametersSetAsync()
    {
        if (_loadedWarehouseId == WarehouseId)
        {
            return;
        }

        _loadedWarehouseId = WarehouseId;
        _search = null;
        _activeSearch = null;
        _categoryId = null;
        _status = StockStatusFilter.All;
        _hideZero = false;
        _sort = StockSortColumn.Available;
        _descending = true;
        _pageIndex = 1;

        if (_table is not null)
        {
            await _table.ReloadServerData();
        }
    }

    private async Task<TableData<WarehouseStockRow>> LoadServerDataAsync(
        TableState state,
        CancellationToken tableCancellation)
    {
        if (_disposed)
        {
            return _latestData;
        }

        long version = ++_requestVersion;
        _loadCancellation?.Cancel();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            tableCancellation,
            _lifetime.Token);
        _loadCancellation = cancellation;
        _loading = true;
        _error = null;
        try
        {
            Result<WarehouseStockResult> result = await QueryAsync(cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();

            // A filter or a stock change can leave the current page past the end; re-query the clamped page once.
            if (result.IsSuccess)
            {
                int lastPage = Math.Max(
                    1,
                    (result.Value.TotalCount + result.Value.PageSize - 1) / result.Value.PageSize);
                if (_pageIndex > lastPage)
                {
                    _pageIndex = lastPage;
                    result = await QueryAsync(cancellation.Token);
                    cancellation.Token.ThrowIfCancellationRequested();
                }
            }

            if (version != _requestVersion || _disposed)
            {
                return _latestData;
            }

            if (!result.IsSuccess)
            {
                _stock = null;
                _error = result.ErrorMessage;
                _totalCount = 0;
                _crumbs = _defaultCrumbs;
                _latestData = new TableData<WarehouseStockRow> { Items = [], TotalItems = 0 };
                return _latestData;
            }

            WarehouseStockResult stock = result.Value;
            _stock = stock;
            _totalCount = stock.TotalCount;
            _pageSize = stock.PageSize;
            _crumbs =
            [
                new("المخزون", "/inventory"),
                new(stock.Header.Name)
            ];
            _latestData = new TableData<WarehouseStockRow>
            {
                Items = stock.Rows,
                TotalItems = stock.TotalCount
            };
            return _latestData;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            return _latestData;
        }
        catch (AuthorizationDeniedException)
        {
            if (version == _requestVersion && !_disposed)
            {
                Navigation.NavigateTo("/access-denied");
            }

            return _latestData;
        }
        catch (Exception exception)
        {
            if (version == _requestVersion && !_disposed)
            {
                Logger.LogError(exception, "Failed to load stock for warehouse {WarehouseId}", WarehouseId);
                _stock = null;
                _error = "تعذر تحميل أرصدة المخزن. يرجى المحاولة مرة أخرى.";
                _totalCount = 0;
                _latestData = new TableData<WarehouseStockRow> { Items = [], TotalItems = 0 };
            }

            return _latestData;
        }
        finally
        {
            if (version == _requestVersion && !_disposed)
            {
                _loading = false;
                await InvokeAsync(StateHasChanged);
            }

            if (ReferenceEquals(_loadCancellation, cancellation))
            {
                _loadCancellation = null;
            }
        }
    }

    private Task<Result<WarehouseStockResult>> QueryAsync(CancellationToken cancellationToken) =>
        Executor.ExecuteAsync(
            new GetWarehouseStockQuery(
                WarehouseId,
                _activeSearch,
                _categoryId,
                _status,
                _hideZero,
                _sort,
                _descending,
                _pageIndex,
                _pageSize),
            cancellationToken);

    private async Task SearchChangedAsync(string? text)
    {
        if (_disposed)
        {
            return;
        }

        _search = text;
        ++_requestVersion;
        _loadCancellation?.Cancel();
        _searchDelay?.Cancel();
        using var delay = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _searchDelay = delay;
        try
        {
            await Task.Delay(400, delay.Token);
            delay.Token.ThrowIfCancellationRequested();
            _activeSearch = text;
            _pageIndex = 1;
            if (_table is not null && !_disposed)
            {
                await _table.ReloadServerData();
            }
        }
        catch (OperationCanceledException) when (delay.IsCancellationRequested)
        {
            // Superseded input or disposal: do not execute delayed work.
        }
        finally
        {
            if (ReferenceEquals(_searchDelay, delay))
            {
                _searchDelay = null;
            }
        }
    }

    private async Task OnCategoryChangedAsync(int? categoryId)
    {
        _categoryId = categoryId;
        await ReloadFromFirstPageAsync();
    }

    private async Task OnStatusChangedAsync(StockStatusFilter status)
    {
        _status = status;
        await ReloadFromFirstPageAsync();
    }

    private async Task OnHideZeroChangedAsync(bool hideZero)
    {
        _hideZero = hideZero;
        await ReloadFromFirstPageAsync();
    }

    private async Task ToggleSortAsync(StockSortColumn column)
    {
        if (_disposed)
        {
            return;
        }

        if (_sort == column)
        {
            _descending = !_descending;
        }
        else
        {
            _sort = column;
            _descending = column != StockSortColumn.ProductName;
        }

        await ReloadFromFirstPageAsync();
    }

    private async Task ReloadFromFirstPageAsync()
    {
        if (_disposed)
        {
            return;
        }

        _pageIndex = 1;
        if (_table is not null)
        {
            await _table.ReloadServerData();
        }
    }

    private async Task RefreshAsync()
    {
        if (_disposed || _table is null)
        {
            return;
        }

        await _table.ReloadServerData();
    }

    private async Task OnFooterPageChanged(int page)
    {
        _pageIndex = Math.Max(1, page);
        if (_table is not null)
        {
            await _table.ReloadServerData();
        }
    }

    private async Task OnFooterRowsPerPageChanged(int rowsPerPage)
    {
        _pageSize = rowsPerPage;
        _pageIndex = 1;
        if (_table is not null)
        {
            await _table.ReloadServerData();
        }
    }

    private async Task OpenLedgerAsync(WarehouseStockRow row)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            DialogParameters<LedgerDialog> parameters = new()
            {
                { dialog => dialog.WarehouseId, WarehouseId },
                { dialog => dialog.ProductId, row.ProductId },
                { dialog => dialog.ProductName, row.ProductName },
                { dialog => dialog.ProductCode, row.ProductCode }
            };
            DialogOptions options = new()
            {
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                CloseButton = false,
                CloseOnEscapeKey = true,
                BackdropClick = true
            };

            await Dialogs.ShowAsync<LedgerDialog>("سجل حركات المنتج", parameters, options);
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(
                exception,
                "Failed to open the ledger dialog for product {ProductId} in warehouse {WarehouseId}",
                row.ProductId,
                WarehouseId);
        }
    }

    private string SortIcon(StockSortColumn column) =>
        _sort != column
            ? Icons.Material.Filled.UnfoldMore
            : _descending
                ? Icons.Material.Filled.ArrowDownward
                : Icons.Material.Filled.ArrowUpward;

    private static string StatusTag(StockStatusFilter status) => status switch
    {
        StockStatusFilter.Available => "border-emerald-200 bg-emerald-50 text-emerald-700",
        StockStatusFilter.PartiallyReserved => "border-sky-200 bg-sky-50 text-sky-700",
        StockStatusFilter.FullyReserved => "border-amber-200 bg-amber-50 text-amber-800",
        StockStatusFilter.OutOfStock => "border-slate-200 bg-slate-100 text-slate-600",
        _ => "border-slate-200 bg-white text-slate-600"
    };

    private static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        ++_requestVersion;
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
}
