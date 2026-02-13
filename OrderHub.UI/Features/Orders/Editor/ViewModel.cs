using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using OrderHub.UI.Common;
using OrderHub.UI.Features.Orders.Editor.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ClientDtos;
using static OrderHub.Application.DTOs.CommonDtos;
using static OrderHub.Application.DTOs.ProductDtos;

namespace OrderHub.UI.Features.Orders.Editor;

internal abstract partial class ViewModel : EditorViewModelBase
{
    private readonly IMediator _mediator;
    private ObservableCollection<KeyValuePair<CategoryInfoDto, IEnumerable<CategoryInfoDto>>> _subCategories;
    private ObservableCollection<OrderItem> _orderItems = new();

    public ViewModel(IMediator mediator)
    {
        _mediator = mediator;
        _subCategories = [];
        OrderBuilder = new OrderBuilder();
        ValidateAllProperties();
    }

    public OrderBuilder OrderBuilder { get; }

    [ObservableProperty]
    private IEnumerable<CategoryInfoDto> _rootCategories;

    [ObservableProperty]
    private CategoryInfoDto _selectedCategory;

    public ObservableCollection<KeyValuePair<CategoryInfoDto, IEnumerable<CategoryInfoDto>>> SubCategories
    {
        get => _subCategories;
        set => SetProperty(ref _subCategories, value);
    }

    [ObservableProperty]
    private IEnumerable<ClientListDto> _clients;

    [ObservableProperty]
    [Required(ErrorMessage = "العميل مطلوب")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private ClientListDto _selectedClient;

    [ObservableProperty]
    private IEnumerable<ProductListDto> _products;

    [ObservableProperty]
    [Required(ErrorMessage = "المنتج مطلوب")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(AddProductCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetProductPriceCommand))]
    [NotifyPropertyChangedFor(nameof(HasProductSelected))]
    private ProductListDto _selectedProduct;

    [ObservableProperty]
    [Required(ErrorMessage = "السعر مطلوب")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(AddProductCommand))]
    private decimal _price;

    [ObservableProperty]
    [Required(ErrorMessage = "الكمية مطلوبة")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(AddProductCommand))]
    private decimal _quantity = 1;

    [ObservableProperty]
    private decimal _subTotal;

    public bool HasProductSelected => SelectedProduct is not null;

    public IEnumerable<OrderItem> OrderItems
    {
        get => _orderItems;
        private set => SetProperty(ref _orderItems, new ObservableCollection<OrderItem>(value));
    }

    internal async Task LoadAsync()
    {
        RootCategories = await _mediator.Send(new Application.Queries.CommonQueries.GetRootCategoriesQuery());
        Clients = await _mediator.Send(new Application.Queries.ClientQueries.GetAllClientsQuery());
    }

    [RelayCommand(CanExecute = nameof(HasProductSelected))]
    private void ResetProductPrice()
    {
        Price = SelectedProduct.Price;
    }

    [RelayCommand(CanExecute = nameof(CanAddProduct))]
    private void AddProduct(ProductListDto product)
    {
        OrderItem item = new()
        {
            ProductName = product.Name,
            Price = Price,
            Quantity = Quantity,
            CategoryName = product.CategoryName
        };
        OrderBuilder.AddItem(item);
        ClearSelectedProduct();
    }

    [RelayCommand]
    private void RemoveOrderItem(OrderItem item) => OrderBuilder.RemoveItem(item);

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
            RemoveSubCategoriesAfterParent((int)newValue.ParentId);
        }

        IEnumerable<CategoryInfoDto> subCategories = await _mediator.Send(new Application.Queries.CommonQueries.GetSubCategoriesQuery(newValue.Id));

        bool exists = _subCategories.Any(s => s.Key.Id == newValue.Id);

        if (subCategories.Any() && !exists)
        {
            SubCategories.Add(new KeyValuePair<CategoryInfoDto, IEnumerable<CategoryInfoDto>>(newValue, subCategories));
        }
    }

    async partial void OnSelectedCategoryChanged(CategoryInfoDto oldValue, CategoryInfoDto newValue)
    {
        Products = await _mediator.Send(new Application.Queries.ProductQueries.GetProductsByCategoryQuery(newValue.Id));
    }

    partial void OnSelectedProductChanged(ProductListDto oldValue, ProductListDto newValue)
    {
        Price = newValue is null ? 0 : newValue.Price;
    }

    partial void OnPriceChanged(decimal oldValue, decimal newValue)
    {
        SubTotal = Price * Quantity;
    }

    partial void OnQuantityChanged(decimal oldValue, decimal newValue)
    {
        SubTotal = Price * Quantity;
    }

    private void RemoveSubCategoriesAfterParent(int parentId)
    {
        List<int> ids = SubCategories.Select(c => c.Key.Id).ToList();

        int parentIndex = ids.IndexOf(parentId);

        if (parentIndex + 1 < ids.Count)
        {
            for (int i = parentIndex + 1; i < ids.Count; i++)
            {
                SubCategories.RemoveAt(i);
            }
        }
    }

    private void ClearSelectedProduct() => SelectedProduct = null;

    private bool CanAddProduct() => SelectedProduct is not null && SubTotal > 0;
}