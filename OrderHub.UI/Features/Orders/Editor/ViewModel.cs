using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Microsoft.IdentityModel.Abstractions;
using OrderHub.Domain.Models;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ClientDtos;
using static OrderHub.Application.DTOs.CommonDtos;
using static OrderHub.Application.DTOs.PaymentMothodsDtos;
using static OrderHub.Application.DTOs.ProductDtos;

namespace OrderHub.UI.Features.Orders.Editor;

internal abstract partial class ViewModel : EditorViewModelBase
{
    protected IMediator _mediator;
    protected readonly IDialogService _dialogService;
    protected readonly IMessenger _messenger;
    private ObservableCollection<KeyValuePair<CategoryInfoDto, IEnumerable<CategoryInfoDto>>> _subCategories;
    private ObservableCollection<OrderItemViewModel> _orderItems = new();

    public ViewModel(IMediator mediator, IDialogService dialogService, IMessenger messenger)
    {
        _mediator = mediator;
        _dialogService = dialogService;
        _messenger = messenger;
        _messenger.Register<Application.Messages.Clients.ClientCreatedMessage>(this, async (r, m) =>
        {
            Clients = await _mediator.Send(new Application.Queries.ClientQueries.GetAllClientsQuery());
        });
        _messenger.Register<Application.Messages.Categories.CategoryCreatedMessage>(this, async (r, m) =>
        {
            RootCategories = await _mediator.Send(new Application.Queries.CommonQueries.GetRootCategoriesQuery());
        });
        _subCategories = [];
        OrderBuilder = new OrderBuilder();
        //SuppliersViewModel = new SuppliersViewModel();
        OrderBuilder.ItemsChanged += (s, e) => SaveCommand.NotifyCanExecuteChanged();
        DeliveryMethodsViewModel = new DeliveryMethodsViewModel();
        DeliveryMethodsViewModel.ErrorsChanged += DeliveryMethodsViewModel_ErrorsChanged;
        
        ValidateAllProperties();
    }

    private void SuppliersViewModel_ErrorsChanged(object sender, DataErrorsChangedEventArgs e)
    {
        AddProductCommand.NotifyCanExecuteChanged();
    }

    private void DeliveryMethodsViewModel_ErrorsChanged(object sender, DataErrorsChangedEventArgs e)
    {
        SaveCommand.NotifyCanExecuteChanged();
    }

    public OrderBuilder OrderBuilder { get; }
    public abstract string ActionName { get; }

    public DeliveryMethodsViewModel DeliveryMethodsViewModel { get; }

    [ObservableProperty]
    private IEnumerable<CategoryInfoDto> _rootCategories;

    [ObservableProperty]
    private string _searchTerm;

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
    [NotifyCanExecuteChangedFor(nameof(AddProductCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetProductPriceCommand))]
    [NotifyPropertyChangedFor(nameof(HasProductSelected))]
    private ProductListDto _selectedProduct;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddProductCommand))]
    private decimal _price;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddProductCommand))]
    private decimal _quantity = 1;

    [ObservableProperty]
    private decimal _subTotal;

    [ObservableProperty]
    private IEnumerable<PaymentMethodListDto> _paymentMethods;
    [ObservableProperty]
    private PaymentMethodListDto _selectedPaymentMethod;

    public bool HasProductSelected => SelectedProduct is not null;

    public IEnumerable<OrderItemViewModel> OrderItems
    {
        get => _orderItems;
        private set => SetProperty(ref _orderItems, new ObservableCollection<OrderItemViewModel>(value));
    }

    internal async Task LoadAsync()
    {
        DeliveryMethodsViewModel.Deliverymen = await _mediator.Send(new Application.Queries.CommonQueries.GetAllDeliverymenInfoQuery());
        DeliveryMethodsViewModel.ShippingCarriers = await _mediator.Send(new Application.Queries.CommonQueries.GetAllShippingCarriersInfoQuery());

        RootCategories = await _mediator.Send(new Application.Queries.CommonQueries.GetRootCategoriesQuery());
        PaymentMethods = await _mediator.Send(new Application.Queries.PaymentMothodQueries.GetPaymentMethodListQuery());
        Clients = await _mediator.Send(new Application.Queries.ClientQueries.GetAllClientsQuery());
    }

    public override bool CanSave => base.CanSave && !DeliveryMethodsViewModel.HasErrors;

    [RelayCommand(CanExecute = nameof(HasProductSelected))]
    private void ResetProductPrice()
    {
        Price = SelectedProduct.Price;
    }

    [RelayCommand(CanExecute = nameof(CanAddProduct))]
    private void AddProduct(ProductListDto product)
    {
        OrderItemViewModel item = new()
        {
            ProductName = product.Name,
            ProductId = product.Id,
            Price = Price,
            Quantity = Quantity,
            CategoryName = product.CategoryName,
            Suppliers = product.Suppliers.Select(s => new OrderItemSupplier(s.Id, s.Name))
        };
        OrderBuilder.AddItem(item);
        ClearSelectedProduct();
    }

    private bool _orderCreated;
    public bool OrderCreated
    {
        get => _orderCreated;
        protected set => SetProperty(ref _orderCreated, value);
    }

    [RelayCommand]
    private void ShowCreateClient() => _dialogService.ShowDialog<Features.Clients.Create.View>();

    [RelayCommand]
    private void RemoveOrderItem(OrderItemViewModel item) => OrderBuilder.RemoveItem(item);

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

    async partial void OnSearchTermChanged(string oldValue, string newValue)
    {
        Products = await _mediator.Send(new Application.Queries.ProductQueries.GetProductsByNameQuery(newValue));
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

