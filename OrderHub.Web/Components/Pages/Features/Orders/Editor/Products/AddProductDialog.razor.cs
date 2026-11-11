
using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderHub.Application.Common;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Features.Products.Search;
using OrderHub.Application.Interfaces;
using System.Globalization;

namespace OrderHub.Web.Components.Pages.Features.Orders.Editor.Products;

public partial class AddProductDialog : IDisposable
{
    MudTextField<string>? _productSearchField;
    [Inject]
    private IRequestExecutor RequestExecutor { get; set; } = null!;

    [Inject]
    private ILogger<AddProductDialog> Logger { get; set; } = null!;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter, EditorRequired]
    public Func<ProductLookupItem, Task<OrderHub.Domain.Common.Result>> ProductSelected { get; set; } = null!;

    private string? _searchTerm;
    private IReadOnlyList<ProductLookupItem> _products = [];

    private const int SEARCH_DEBOUNCE_MILLISECONDS = 300;
    private const int PAGE_SIZE = 10;

    private CancellationTokenSource? _searchCancellation;
    private bool _isDisposed;
    private bool _isAdding;
    private bool _isSearching;
    private bool _isLoadingMore;

    private int _pageNumber;
    private int _totalCount;
    private bool _hasNextPage;

    private string? _searchError;
    private string? _loadMoreError;
    private string? _addError;
    private ProductLookupItem? _selectedProduct;

    private bool CanContinue =>
        !_isAdding && !_isSearching && _selectedProduct is not null;

    private bool ShowLoadMoreButton =>
        !_isSearching &&
        !_isLoadingMore &&
        _searchError is null &&
        _loadMoreError is null &&
        _hasNextPage;

    private string DisplayedCountText =>
        $"أُعرض {_products.Count.ToString("N0", CultureInfo.InvariantCulture)} من {_totalCount.ToString("N0", CultureInfo.InvariantCulture)}";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await Task.Delay(200);

        if (_productSearchField is not null)
            await _productSearchField.FocusAsync();

        if (firstRender)
            await SearchAsync(string.Empty, debounced: false);
    }

    private async Task TextChanged(string value) =>
        await SearchAsync(value, debounced: true);

    private async Task SearchAsync(string value, bool debounced)
    {
        if (_isDisposed)
            return;

        _searchTerm = value;

        var previousCancellation = _searchCancellation;
        _searchCancellation = null;
        previousCancellation?.Cancel();

        _products = [];
        _selectedProduct = null;
        _searchError = null;
        _loadMoreError = null;
        _isSearching = false;
        _isLoadingMore = false;

        var cancellation = new CancellationTokenSource();
        _searchCancellation = cancellation;
        _isSearching = true;

        try
        {
            if (debounced)
                await Task.Delay(
                    SEARCH_DEBOUNCE_MILLISECONDS,
                    cancellation.Token);

            var page = await RequestExecutor.ExecuteAsync(
                new SearchProductsQuery(_searchTerm ?? string.Empty, 1, PAGE_SIZE),
                cancellation.Token);

            if (!_isDisposed &&
                ReferenceEquals(_searchCancellation, cancellation))
            {
                ApplyPage(page, append: false);
            }
        }
        catch (OperationCanceledException)
            when (cancellation.IsCancellationRequested)
        {
            // تم استبدال البحث بطلب أحدث أو إغلاق النافذة.
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Product lookup failed.");

            if (!_isDisposed &&
                ReferenceEquals(_searchCancellation, cancellation))
            {
                _searchError = "تعذر تحميل المنتجات. حاول مرة أخرى.";
            }
        }
        finally
        {
            if (ReferenceEquals(_searchCancellation, cancellation))
            {
                _searchCancellation = null;
                _isSearching = false;
            }

            cancellation.Dispose();
        }
    }

    private async Task LoadMoreAsync()
    {
        if (_isDisposed ||
            _isAdding ||
            _isSearching ||
            _isLoadingMore ||
            !_hasNextPage)
        {
            return;
        }

        var previousCancellation = _searchCancellation;
        _searchCancellation = null;
        previousCancellation?.Cancel();

        _loadMoreError = null;

        var cancellation = new CancellationTokenSource();
        _searchCancellation = cancellation;
        _isLoadingMore = true;

        try
        {
            var page = await RequestExecutor.ExecuteAsync(
                new SearchProductsQuery(_searchTerm ?? string.Empty, _pageNumber + 1, PAGE_SIZE),
                cancellation.Token);

            if (!_isDisposed &&
                ReferenceEquals(_searchCancellation, cancellation))
            {
                ApplyPage(page, append: true);
            }
        }
        catch (OperationCanceledException)
            when (cancellation.IsCancellationRequested)
        {
            // تم استبدال التحميل ببحث أحدث أو إغلاق النافذة.
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Loading the next product page failed.");

            if (!_isDisposed &&
                ReferenceEquals(_searchCancellation, cancellation))
            {
                _loadMoreError = "تعذر تحميل المزيد من المنتجات. حاول مرة أخرى.";
            }
        }
        finally
        {
            if (ReferenceEquals(_searchCancellation, cancellation))
            {
                _searchCancellation = null;
                _isLoadingMore = false;
            }

            cancellation.Dispose();
        }
    }

    private void ApplyPage(PagedResult<ProductLookupItem> page, bool append)
    {
        _pageNumber = page.PageNumber;
        _totalCount = page.TotalCount;
        _hasNextPage = page.HasNextPage;

        _products = append
            ? _products.Concat(page.Items).ToList()
            : page.Items.ToList();

        // OnAfterRenderAsync لا يُعيد رسم الواجهة تلقائيًا بعد اكتمال
        // العمل غير المتزامن، لذلك نطلب الرسم صراحةً حتى تظهر القائمة
        // فور فتح النافذة دون انتظار تدخل المستخدم.
        StateHasChanged();
    }

    private void SelectProduct(ProductLookupItem? product)
    {
        if (_isAdding)
            return;

        _selectedProduct = product;
    }

    private void Cancel()
    {
        if (!_isAdding)
            MudDialog.Cancel();
    }

    private async Task ContinueAsync()
    {
        if (!CanContinue || _selectedProduct is null)
            return;

        _isAdding = true;
        _addError = null;

        try
        {
            var result = await ProductSelected(_selectedProduct);
            if (result.IsSuccess)
                MudDialog.Close(DialogResult.Ok(_selectedProduct));
            else
                _addError = result.ErrorMessage;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to open the editor for the selected product.");
            _addError = "تعذر فتح محرر المنتج. حاول مرة أخرى.";
        }
        finally
        {
            _isAdding = false;
        }
    }

    public void Dispose()
    {
        _isDisposed = true;

        var cancellation = _searchCancellation;
        _searchCancellation = null;

        cancellation?.Cancel();
    }
}
