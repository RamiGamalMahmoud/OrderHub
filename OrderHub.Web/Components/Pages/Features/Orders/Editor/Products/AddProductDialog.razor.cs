using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderHub.Application.Features.Products.Contracts;
using System.Threading.Tasks;

namespace OrderHub.Web.Components.Pages.Features.Orders.Editor.Products;

public partial class AddProductDialog
{

    // =========================
    // Services
    // =========================

    [Inject]
    private IProductStore ProductStore { get; set; } = null!;


    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public EventCallback<AddProductResult> ProductAdded { get; set; }


    // =========================
    // Search
    // =========================

    private string? _searchTerm;

    private IReadOnlyList<ProductLookupItem> _products = [];


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
        _unitPrice >= 0 &&
        _quantity > 0 &&
        _vatRate is >= 0 and <= 100;


    // =========================
    // Search
    // =========================

    private async Task TextChanged(string value)
    {
        _searchTerm = value;

        if (string.IsNullOrWhiteSpace(value))
        {
            _products = [];
            return;
        }

        _products = (await ProductStore.GetProductsByName(value))
            .ToList();
    }


    // =========================
    // Product Selection
    // =========================

    private void SelectProduct(ProductLookupItem product)
    {
        if (_isAdding)
            return;

        _selectedProduct = product;

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
        var originalOptions = MudDialog.Options;

        try
        {
            await MudDialog.SetOptionsAsync(originalOptions with
            {
                CloseButton = false,
                CloseOnEscapeKey = false,
                BackdropClick = false
            });

            await ProductAdded.InvokeAsync(result);

            ResetProduct();
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
