
using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Features.Products.Search;
using OrderHub.Application.Interfaces;

namespace OrderHub.Web.Components.Pages.Features.Orders.Editor.Products;

public partial class AddProductDialog : IDisposable
{
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

    private CancellationTokenSource? _searchCancellation;
    private bool _isDisposed;
    private bool _isAdding;
    private bool _isSearching;

    private string? _searchError;
    private string? _addError;
    private ProductLookupItem? _selectedProduct;

    private bool CanContinue =>
        !_isAdding && !_isSearching && _selectedProduct is not null;

    private async Task TextChanged(string value)
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
        _isSearching = false;

        if (string.IsNullOrWhiteSpace(value))
            return;

        var cancellation = new CancellationTokenSource();
        _searchCancellation = cancellation;
        _isSearching = true;

        try
        {
            await Task.Delay(
                SEARCH_DEBOUNCE_MILLISECONDS,
                cancellation.Token);

            var products = await RequestExecutor.ExecuteAsync(
                new SearchProductsQuery(value),
                cancellation.Token);

            if (!_isDisposed &&
                ReferenceEquals(_searchCancellation, cancellation))
            {
                _products = products.ToList();
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
                _searchError = "تعذر البحث عن المنتجات. حاول مرة أخرى.";
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
                _selectedProduct = null;
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
