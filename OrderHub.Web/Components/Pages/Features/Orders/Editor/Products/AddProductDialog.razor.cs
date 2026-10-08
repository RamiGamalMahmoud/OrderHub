using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Features.Products.Search;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using System.Threading.Tasks;

namespace OrderHub.Web.Components.Pages.Features.Orders.Editor.Products;

public partial class AddProductDialog : IDisposable
{

    // =========================
    // Services
    // =========================

    [Inject]
    private IRequestExecutor RequestExecutor { get; set; } = null!;

    [Inject]
    private ILogger<AddProductDialog> Logger { get; set; } = null!;


    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public Func<AddProductResult, Task<Result>> ProductAdded { get; set; } = null!;


    // =========================
    // Search
    // =========================

    private string? _searchTerm;

    private IReadOnlyList<ProductLookupItem> _products = [];

    private const int SearchDebounceMilliseconds = 300;

    private CancellationTokenSource? _searchCancellation;

    private bool _isDisposed;

    private string? _searchError;

    private string? _addError;


    // =========================
    // Selected Product
    // =========================

    private ProductLookupItem? _selectedProduct;


    // =========================
    // Order Item
    // =========================

    private decimal _unitPrice;

    private int _quantity = 0;

    private decimal _vatRate;

    private bool _isAdding;


    // =========================
    // Calculations
    // =========================

    private decimal SubTotal =>
        _unitPrice * _quantity;


    private decimal VatAmount =>
        SubTotal * (_vatRate / 100m);


    private decimal Total =>
        SubTotal + VatAmount;


    private bool CanAdd =>
        !_isAdding &&
        _selectedProduct is not null &&
        _unitPrice > 0 &&
        _quantity > 0 &&
        _vatRate is >= 0 and <= 100;


    // =========================
    // Search
    // =========================

    private async Task TextChanged(string value)
    {
        if (_isDisposed)
            return;

        _searchTerm = value;
        _searchCancellation?.Cancel();
        _searchCancellation = null;
        _products = [];
        _searchError = null;

        if (string.IsNullOrWhiteSpace(value))
            return;

        using var cancellation = new CancellationTokenSource();
        _searchCancellation = cancellation;

        try
        {
            await Task.Delay(SearchDebounceMilliseconds, cancellation.Token);

            var products = await RequestExecutor.ExecuteAsync(new SearchProductsQuery(value), cancellation.Token);

            // Request identity protects ordering even if the store ignores cancellation.
            if (!_isDisposed && ReferenceEquals(_searchCancellation, cancellation))
                _products = products.ToList();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // A newer search or dialog disposal superseded this request.
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Product lookup failed.");
            if (!_isDisposed && ReferenceEquals(_searchCancellation, cancellation))
                _searchError = "تعذر البحث عن المنتجات. حاول مرة أخرى.";
        }
        finally
        {
            if (ReferenceEquals(_searchCancellation, cancellation))
                _searchCancellation = null;
        }
    }

    public void Dispose()
    {
        _isDisposed = true;
        var cancellation = _searchCancellation;
        _searchCancellation = null;
        cancellation?.Cancel();
    }


    // =========================
    // Product Selection
    // =========================

    private void SelectProduct(ProductLookupItem product)
    {
        if (_isAdding)
            return;

        _selectedProduct = product;
        _addError = null;

        _unitPrice = product.Price;
        _quantity = 0;
        _vatRate = 0;
    }


    // =========================
    // Dialog
    // =========================

    private void Cancel()
    {
        if (_isAdding)
            return;

        MudDialog.Cancel();
    }


    private async Task Add()
    {
        if (!CanAdd)
            return;

        AddProductResult result = new AddProductResult(
            Product: _selectedProduct!,
            UnitPrice: _unitPrice,
            Quantity: _quantity,
            VatRate: _vatRate);

        _isAdding = true;
        _addError = null;
        var originalOptions = MudDialog.Options;

        try
        {
            await MudDialog.SetOptionsAsync(originalOptions with
            {
                CloseButton = false,
                CloseOnEscapeKey = false,
                BackdropClick = false
            });

            var outcome = await ProductAdded(result);
            if (outcome.IsSuccess)
                ResetProduct();
            else
                _addError = outcome.ErrorMessage;
        }
        finally
        {
            _isAdding = false;
            await MudDialog.SetOptionsAsync(originalOptions);
        }
    }

    private void ResetProduct()
    {
        _selectedProduct = null;
        _unitPrice = 0;
        _quantity = 0;
        _vatRate = 0;
    }
}
