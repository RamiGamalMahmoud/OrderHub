using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.UI.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CommonDtos;

namespace OrderHub.UI.Features.Orders.Editor;

internal partial class OrderProductsPanelViewModel : ObservableObject
{
    private readonly IProductStore _productStore;

    private CancellationTokenSource _searchCts;
    private CancellationTokenSource _categoryCts;

    public OrderProductsPanelViewModel(
        IMediator mediator,
        IProductStore productStore)
    {
        _productStore = productStore;

        CategorySelection = new CategorySelection(mediator);
        CategorySelection.SelectedCategoryChanged += CategorySelection_OnSelectedCategoryChanged;
    }

    public event Action<ProductSelectedEventArgs> ProductSelected;

    public CategorySelection CategorySelection { get; }

    [ObservableProperty]
    private IEnumerable<ProductItem> _products;

    [ObservableProperty]
    private string _searchTerm;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddProductCommand))]
    private CategoryInfoDto _selectedCategory;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddProductCommand))]
    private ProductItem _selectedProduct;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddProductCommand))]
    [NotifyPropertyChangedFor(nameof(SubTotal))]
    private decimal _price;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddProductCommand))]
    [NotifyPropertyChangedFor(nameof(SubTotal))]
    private int _quantity;

    public decimal SubTotal => Price * Quantity;

    public Task LoadAsync()
        => ReloadRootCategoriesAsync();

    public async Task ReloadRootCategoriesAsync()
    {
        await CategorySelection.LoadRootCategoriesAsync();
    }

    [RelayCommand(CanExecute = nameof(CanAddProduct))]
    private void AddProduct()
    {
        if (SelectedProduct is null)
            return;

        OnProductSelected(new ProductSelectedEventArgs(
            SelectedProduct.Id,
            Price,
            Quantity));

        ClearSelection();
    }

    [RelayCommand]
    private void ResetProductPrice()
    {
        Price = SelectedProduct?.Price ?? 0;
    }

    async partial void OnSearchTermChanged(string oldValue, string newValue)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();

        CancellationToken token = _searchCts.Token;

        try
        {
             await Task.Delay(400, token);

            IEnumerable<ProductLookupItem> searchProducts =
                await _productStore.GetProductsByName(SearchTerm);

            Products = searchProducts.Select(product =>
                new ProductItem(
                    product.Id,
                    product.Name,
                    product.Price,
                    product.CategoryName,
                    product.Suppliers.Select(supplier =>
                        new ProductSupplierItem(
                            supplier.Id,
                            supplier.Name))));
        }
        catch (OperationCanceledException)
        {
        }
    }

    partial void OnSelectedProductChanged(ProductItem value)
    {
        Price = value?.Price ?? 0;
    }

    private async void CategorySelection_OnSelectedCategoryChanged(
        object sender,
        CategoryInfoDto category)
    {
        if (category is null)
            return;

        _categoryCts?.Cancel();
        _categoryCts = new CancellationTokenSource();

        try
        {
            IEnumerable<ProductLookupItem> products =
                await _productStore.GetProductByCategoryAsync(category.Id);

            Products = products.Select(product =>
                new ProductItem(
                    product.Id,
                    product.Name,
                    product.Price,
                    product.CategoryName,
                    product.Suppliers.Select(supplier =>
                        new ProductSupplierItem(
                            supplier.Id,
                            supplier.Name))));
        }
        catch (OperationCanceledException)
        {
        }
    }

    private bool CanAddProduct()
        => SelectedProduct is not null && SubTotal > 0;

    private void OnProductSelected(ProductSelectedEventArgs e)
    {
        ProductSelected?.Invoke(e);
    }

    private void ClearSelection()
    {
        SelectedProduct = null;
        Quantity = 0;
    }
}

public record ProductItem(
    int Id,
    string Name,
    decimal Price,
    string CategoryName,
    IEnumerable<ProductSupplierItem> Suppliers);

public record ProductSupplierItem(
    int Id,
    string Name);

public class ProductSelectedEventArgs : EventArgs
{
    public ProductSelectedEventArgs(
        int id,
        decimal price,
        int quantity)
    {
        Id = id;
        Price = price;
        Quantity = quantity;
    }

    public int Id { get; }

    public decimal Price { get; }

    public int Quantity { get; }
}

public delegate void ProductSelectedEventHandler(
    object sender,
    ProductSelectedEventArgs e);