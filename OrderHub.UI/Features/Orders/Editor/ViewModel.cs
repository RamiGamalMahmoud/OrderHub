using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ClientDtos;
using static OrderHub.Application.DTOs.CommonDtos;
using static OrderHub.Application.DTOs.PaymentMothodsDtos;
using static OrderHub.Application.DTOs.ProductDtos;

namespace OrderHub.UI.Features.Orders.Editor;

internal abstract partial class ViewModel : EditorViewModelBase
{
    protected readonly IMediator _mediator;
    protected readonly IDialogService _dialogService;
    protected readonly IMessenger _messenger;

    private CancellationTokenSource _searchCts;
    private CancellationTokenSource _categoryCts;

    public OrderBuilder OrderBuilder { get; }
    public ObservableCollection<OrderItemViewModel> OrderItems => OrderBuilder.Items;

    public DeliveryMethodsViewModel DeliveryMethodsViewModel { get; }

    public abstract string ActionName { get; }

    public ViewModel(IMediator mediator, IDialogService dialogService, IMessenger messenger)
    {
        _mediator = mediator;
        _dialogService = dialogService;
        _messenger = messenger;

        OrderBuilder = new OrderBuilder();
        DeliveryMethodsViewModel = new DeliveryMethodsViewModel();

        OrderBuilder.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(OrderBuilder.TotalPrice))
                SaveCommand.NotifyCanExecuteChanged();
        };

        DeliveryMethodsViewModel.ErrorsChanged += (_, _) =>
            SaveCommand.NotifyCanExecuteChanged();

        RegisterMessages();
    }

    private void RegisterMessages()
    {
        _messenger.Register<Application.Messages.Clients.ClientCreatedMessage>(
            this,
            async (_, _) => Clients = await _mediator.Send(
                new Application.Queries.ClientQueries.GetAllClientsQuery()));

        _messenger.Register<Application.Messages.Categories.CategoryCreatedMessage>(
            this,
            async (_, _) => RootCategories = await _mediator.Send(
                new Application.Queries.CommonQueries.GetRootCategoriesQuery()));
    }

    [ObservableProperty] private IEnumerable<CategoryInfoDto> _rootCategories;
    [ObservableProperty] private ObservableCollection<KeyValuePair<CategoryInfoDto, IEnumerable<CategoryInfoDto>>> _subCategories =[];
    [ObservableProperty] private IEnumerable<ClientListDto> _clients;
    [ObservableProperty] private IEnumerable<ProductListDto> _products;
    [ObservableProperty] private IEnumerable<PaymentMethodListDto> _paymentMethods;
    [ObservableProperty] private PaymentMethodListDto _selectedPaymentMethod;
    [ObservableProperty] private string _searchTerm;
    [ObservableProperty] private CategoryInfoDto _selectedCategory;

    [ObservableProperty]
    [Required]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private ClientListDto _selectedClient;

    [ObservableProperty] private ProductListDto _selectedProduct;
    [ObservableProperty] private decimal _price;
    [ObservableProperty] private decimal _quantity = 1;

    public decimal SubTotal => Price * Quantity;

    public bool HasProductSelected => SelectedProduct is not null;

    public override bool CanSave =>
        base.CanSave &&
        !DeliveryMethodsViewModel.HasErrors &&
        OrderBuilder.Items.Any();

    internal async Task LoadAsync()
    {
        var deliverymenTask = _mediator.Send(new Application.Queries.CommonQueries.GetAllDeliverymenInfoQuery());
        var carriersTask = _mediator.Send(new Application.Queries.CommonQueries.GetAllShippingCarriersInfoQuery());
        var categoriesTask = _mediator.Send(new Application.Queries.CommonQueries.GetRootCategoriesQuery());
        var paymentsTask = _mediator.Send(new Application.Queries.PaymentMothodQueries.GetPaymentMethodListQuery());
        var clientsTask = _mediator.Send(new Application.Queries.ClientQueries.GetAllClientsQuery());

        await Task.WhenAll(deliverymenTask, carriersTask, categoriesTask, paymentsTask, clientsTask);

        DeliveryMethodsViewModel.Deliverymen = await deliverymenTask;
        DeliveryMethodsViewModel.ShippingCarriers = await carriersTask;
        RootCategories = await categoriesTask;
        PaymentMethods = await paymentsTask;
        Clients = await clientsTask;
    }


    async partial void OnSearchTermChanged(string oldValue, string newValue)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        try
        {
            await Task.Delay(400, token);

            Products = await _mediator.Send(
                new Application.Queries.ProductQueries.GetProductsByNameQuery(newValue),
                token);
        }
        catch (OperationCanceledException) { }
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
            RemoveSubCategoriesAfterParent((int)newValue.ParentId);
        }

        IEnumerable<CategoryInfoDto> subCategories = await _mediator.Send(new Application.Queries.CommonQueries.GetSubCategoriesQuery(newValue.Id));

        bool exists = _subCategories.Any(s => s.Key.Id == newValue.Id);

        if (subCategories.Any() && !exists)
        {
            SubCategories.Add(new KeyValuePair<CategoryInfoDto, IEnumerable<CategoryInfoDto>>(newValue, subCategories));
        }
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

    async partial void OnSelectedCategoryChanged(CategoryInfoDto oldValue, CategoryInfoDto newValue)
    {
        if (newValue is null) return;

        _categoryCts?.Cancel();
        _categoryCts = new CancellationTokenSource();

        try
        {
            Products = await _mediator.Send(
                new Application.Queries.ProductQueries.GetProductsByCategoryQuery(newValue.Id),
                _categoryCts.Token);
        }
        catch (OperationCanceledException) { }
    }


    [RelayCommand(CanExecute = nameof(CanAddProduct))]
    private void AddProduct(ProductListDto product)
    {
        var item = new OrderItemViewModel
        {
            ProductName = product.Name,
            ProductId = product.Id,
            Price = Price,
            Quantity = Quantity,
            CategoryName = product.CategoryName,
            Suppliers = product.Suppliers.Select(s => new OrderItemSupplier(s.Id, s.Name))
        };

        OrderBuilder.AddItem(item);
        ClearSelection();
    }

    private bool CanAddProduct() => SelectedProduct != null && SubTotal > 0;

    [RelayCommand]
    private void RemoveOrderItem(OrderItemViewModel item)
        => OrderBuilder.RemoveItem(item);

    [RelayCommand]
    private void ResetProductPrice()
        => Price = SelectedProduct?.Price ?? 0;

    partial void OnSelectedProductChanged(ProductListDto oldValue, ProductListDto newValue)
        => Price = newValue?.Price ?? 0;

    partial void OnPriceChanged(decimal oldValue, decimal newValue)
        => OnPropertyChanged(nameof(SubTotal));

    partial void OnQuantityChanged(decimal oldValue, decimal newValue)
        => OnPropertyChanged(nameof(SubTotal));

    private void ClearSelection() => SelectedProduct = null;


    [RelayCommand]
    private void ShowCreateClient()
        => _dialogService.ShowDialog<Features.Clients.Create.View>();
}