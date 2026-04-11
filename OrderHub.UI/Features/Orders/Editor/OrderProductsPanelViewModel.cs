using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CommonDtos;
using static OrderHub.Application.DTOs.ProductDtos;

namespace OrderHub.UI.Features.Orders.Editor;

internal partial class OrderProductsPanelViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly OrderBuilder _orderBuilder;
    private CancellationTokenSource _searchCts;
    private CancellationTokenSource _categoryCts;

    public OrderProductsPanelViewModel(IMediator mediator, OrderBuilder orderBuilder)
    {
        _mediator = mediator;
        _orderBuilder = orderBuilder;
    }

    [ObservableProperty]
    private IEnumerable<CategoryInfoDto> _rootCategories;

    [ObservableProperty]
    private ObservableCollection<KeyValuePair<CategoryInfoDto, IEnumerable<CategoryInfoDto>>> _subCategories = [];

    [ObservableProperty]
    private IEnumerable<ProductListDto> _products;

    [ObservableProperty]
    private string _searchTerm;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddProductCommand))]
    private CategoryInfoDto _selectedCategory;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddProductCommand))]
    private ProductListDto _selectedProduct;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddProductCommand))]
    [NotifyPropertyChangedFor(nameof(SubTotal))]
    private decimal _price;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddProductCommand))]
    [NotifyPropertyChangedFor(nameof(SubTotal))]
    private decimal _quantity = 1;

    public decimal SubTotal => Price * Quantity;

    public Task LoadAsync()
        => ReloadRootCategoriesAsync();

    public async Task ReloadRootCategoriesAsync()
    {
        RootCategories = await _mediator.Send(new Application.Queries.CommonQueries.GetRootCategoriesQuery());
    }

    async partial void OnSearchTermChanged(string oldValue, string newValue)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        CancellationToken token = _searchCts.Token;

        try
        {
            await Task.Delay(400, token);
            Products = await _mediator.Send(
                new Application.Queries.ProductQueries.GetProductsByNameQuery(newValue),
                token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    async partial void OnSelectedCategoryChanging(CategoryInfoDto oldValue, CategoryInfoDto newValue)
    {
        if (newValue is null)
        {
            SubCategories.Clear();
            return;
        }

        if (newValue.ParentId is null)
        {
            SubCategories.Clear();
        }
        else
        {
            RemoveSubCategoriesAfterParent(newValue.ParentId.Value);
        }

        IEnumerable<CategoryInfoDto> subCategories = await _mediator.Send(
            new Application.Queries.CommonQueries.GetSubCategoriesQuery(newValue.Id));

        bool exists = SubCategories.Any(s => s.Key.Id == newValue.Id);

        if (subCategories.Any() && !exists)
        {
            SubCategories.Add(new KeyValuePair<CategoryInfoDto, IEnumerable<CategoryInfoDto>>(newValue, subCategories));
        }
    }

    async partial void OnSelectedCategoryChanged(CategoryInfoDto oldValue, CategoryInfoDto newValue)
    {
        if (newValue is null)
            return;

        _categoryCts?.Cancel();
        _categoryCts = new CancellationTokenSource();

        try
        {
            Products = await _mediator.Send(
                new Application.Queries.ProductQueries.GetProductsByCategoryQuery(newValue.Id),
                _categoryCts.Token);
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

        OrderItemViewModel item = new OrderItemViewModel(_mediator)
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

    partial void OnSelectedProductChanged(ProductListDto oldValue, ProductListDto newValue)
        => Price = newValue?.Price ?? 0;

    private bool CanAddProduct() => SelectedProduct != null && SubTotal > 0;

    private void ClearSelection() => SelectedProduct = null;

    private void RemoveSubCategoriesAfterParent(int parentId)
    {
        List<int> ids = SubCategories.Select(c => c.Key.Id).ToList();
        int parentIndex = ids.IndexOf(parentId);

        if (parentIndex + 1 >= ids.Count)
            return;

        for (int i = ids.Count - 1; i > parentIndex; i--)
        {
            SubCategories.RemoveAt(i);
        }
    }
}
