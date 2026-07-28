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
    private readonly IMediator _mediator;
    private readonly OrderBuilder _orderBuilder;
    private readonly IProductStore _productStore;
    private CancellationTokenSource _searchCts;
    private CancellationTokenSource _categoryCts;

    public OrderProductsPanelViewModel(IMediator mediator, OrderBuilder orderBuilder, IProductStore productStore)
    {
        _mediator = mediator;
        _orderBuilder = orderBuilder;
        CategorySelection = new CategorySelection(mediator);
        CategorySelection.SelectedCategoryChanged += CategorySelection_OnSelectedCategoryChanged;
        _productStore = productStore;
    }

    private async void CategorySelection_OnSelectedCategoryChanged(object sender, CategoryInfoDto e)
    {
        if (e is null)
            return;

        _categoryCts?.Cancel();
        _categoryCts = new CancellationTokenSource();

        try
        {
            IEnumerable<ProductLookupItem> searchProducts = await _productStore.GetProductByCategoryAsync(e.Id);
            Products = searchProducts.Select(x => new ProductItem(x.Id, x.Name, x.Price, x.CategoryName, x.Suppliers.Select(s => new ProductSupplierItem(s.Id, s.Name))));
        }
        catch (OperationCanceledException)
        {
        }
    }

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
    private decimal _quantity = 0;

    public decimal SubTotal => Price * Quantity;

    public Task LoadAsync()
        => ReloadRootCategoriesAsync();

    public async Task ReloadRootCategoriesAsync()
    {
        await CategorySelection.LoadRootCategoriesAsync();
    }

    async partial void OnSearchTermChanged(string oldValue, string newValue)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        CancellationToken token = _searchCts.Token;

        try
        {
            //await Task.Delay(400, token);
            IEnumerable<ProductLookupItem> searchProducts = await _productStore.GetProductsByName(SearchTerm);

            Products = searchProducts.Select(x => new ProductItem(x.Id, x.Name, x.Price, x.CategoryName, x.Suppliers.Select(s => new ProductSupplierItem(s.Id, s.Name))));
        }
        catch (OperationCanceledException)
        {
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddProduct))]
    private void AddProduct()
    {
        if (SelectedProduct is null)
            return;

        OrderItemViewModel item = new()
        {
            ProductName = SelectedProduct.Name,
            ProductId = SelectedProduct.Id,
            Price = Price,
            Quantity = Quantity,
            CategoryName = SelectedProduct.CategoryName,
            Suppliers = SelectedProduct.Suppliers.Select(s => new OrderItemSupplier(s.Id, s.Name))
        };

        _orderBuilder.AddItem(item);
        ClearSelection();
    }

    [RelayCommand]
    private void ResetProductPrice()
        => Price = SelectedProduct?.Price ?? 0;

    partial void OnSelectedProductChanged(ProductItem value)
        => Price = value?.Price ?? 0;

    private bool CanAddProduct() => SelectedProduct != null && SubTotal > 0;

    private void ClearSelection()
    {
        SelectedProduct = null;
        Quantity = 0;
    }
}

public record ProductItem(int Id, string Name,decimal Price, string CategoryName, IEnumerable<ProductSupplierItem> Suppliers);
public record ProductSupplierItem(int Id, string Name);
