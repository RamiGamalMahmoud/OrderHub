using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.DeliverymanDtos;
using static OrderHub.Application.DTOs.ShippingCarriersDtos;

namespace OrderHub.UI.Features.Orders.Editor;

internal abstract partial class ViewModel : EditorViewModelBase
{
    protected readonly IMediator _mediator;
    protected readonly IDialogService _dialogService;
    protected readonly IMessenger _messenger;
    private bool _suspendChangeTracking;

    public OrderBuilder OrderBuilder { get; }
    public ObservableCollection<OrderItemViewModel> OrderItems => OrderBuilder.Items;
    public OrderProductsPanelViewModel ProductsPanel { get; }
    public OrderPartyPanelViewModel PartyPanel { get; }
    public DeliveryMethodsViewModel DeliveryMethodsViewModel { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SelecteNextOrderItemCommand))]
    [NotifyCanExecuteChangedFor(nameof(SelectePrevOrderItemCommand))]
    private OrderItemViewModel _selectedOrderItem;

    public Application.DTOs.ClientDtos.ClientListDto SelectedClient
    {
        get => PartyPanel.SelectedClient;
        set => PartyPanel.SelectedClient = value;
    }

    public Application.DTOs.PaymentMothodsDtos.PaymentMethodListDto SelectedPaymentMethod
    {
        get => PartyPanel.SelectedPaymentMethod;
        set => PartyPanel.SelectedPaymentMethod = value;
    }

    public abstract string ActionName { get; }

    public ViewModel(IMediator mediator, IDialogService dialogService, IMessenger messenger, IProductStore productStore)
    {
        _mediator = mediator;
        _dialogService = dialogService;
        _messenger = messenger;
        OrderBuilder = new OrderBuilder();

        ProductsPanel = new OrderProductsPanelViewModel(mediator, productStore);

        ProductsPanel.ProductSelected += ProductsPanel_ProductSelected;

        PartyPanel = new OrderPartyPanelViewModel(mediator, dialogService);

        DeliveryMethodsViewModel = new DeliveryMethodsViewModel(mediator);

        _notifyPropertiesNames = [];

        OrderBuilder.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(OrderBuilder.TotalPrice))
            {
                MarkAsChanged();
                SaveCommand.NotifyCanExecuteChanged();
            }
        };

        OrderBuilder.Items.CollectionChanged += OrderItems_CollectionChanged;

        PartyPanel.ErrorsChanged += (_, _) => SaveCommand.NotifyCanExecuteChanged();
        PartyPanel.PropertyChanged += (_, _) =>
        {
            MarkAsChanged();
            SaveCommand.NotifyCanExecuteChanged();
        };

        DeliveryMethodsViewModel.ErrorsChanged += (_, _) =>
            SaveCommand.NotifyCanExecuteChanged();

        DeliveryMethodsViewModel.PropertyChanged += (_, _) =>
        {
            MarkAsChanged();
            SaveCommand.NotifyCanExecuteChanged();
        };

        RegisterMessages();
    }

    private void ProductsPanel_ProductSelected(ProductSelectedEventArgs e)
    {
        _ = LoadProducDetails(e.Id, e.Price, e.Quantity);
    }

    private async Task LoadProducDetails(int productId, decimal price, decimal quantity)
    {
        var product = await _mediator.Send(new Application.Features.Orders.GetOrderItemEditor.Query(productId));
        decimal priceDecimal = price == 0 || price != product.Price ? product.Price : price;
        OrderItemViewModel orderItemViewModel = new OrderItemViewModel()
        {
            ProductId = product.ProductId,
            ProductName = product.Name,
            CategoryName = product.CategoryName,
            Price = price,
            Quantity = quantity,

            Suppliers = product.Suppliers
            .Select(s => new OrderItemSupplier(
                s.Id,
                s.Name)),

            Properties = product.Properties
            .Select(p => new OrderItemProperty(
                p.Id,
                p.Name,
                p.IsRequired,
                p.PropertyType,
                p.Options.Select(o => new OrderItemPropertyOption(o.Id, o.Name))))
        };

        OrderItems.Add(orderItemViewModel);
        SelectedOrderItem = orderItemViewModel;
    }

    [RelayCommand(CanExecute = nameof(CanSelectNextOrderItem))]
    private void SelecteNextOrderItem()
    {
        int selectedOrderItemIndex = SelectedOrderItem is null || OrderItems.Count == 0 ? -1 : OrderItems.IndexOf(SelectedOrderItem);
        SelectedOrderItem = OrderItems.ElementAt(selectedOrderItemIndex + 1);
    }

    [RelayCommand(CanExecute = nameof(CanSelectPrevOrderItem))]
    private void SelectePrevOrderItem()
    {
        int selectedOrderItemIndex = SelectedOrderItem is null || OrderItems.Count == 0 ? -1 : OrderItems.IndexOf(SelectedOrderItem);
        SelectedOrderItem = OrderItems.ElementAt(selectedOrderItemIndex - 1);
    }

    private bool CanSelectNextOrderItem()
    {
        int selectedOrderItemIndex = SelectedOrderItem is null || OrderItems.Count == 0 ? -1 : OrderItems.IndexOf(SelectedOrderItem);
        return selectedOrderItemIndex != -1 && OrderItems.Count - 1 > selectedOrderItemIndex;
    }

    private bool CanSelectPrevOrderItem()
    {
        int selectedOrderItemIndex = SelectedOrderItem is null || OrderItems.Count == 0 ? -1 : OrderItems.IndexOf(SelectedOrderItem);
        return selectedOrderItemIndex != -1 && selectedOrderItemIndex > 0;
    }

    private void RegisterMessages()
    {
        _messenger.Register<Application.Messages.Clients.ClientCreatedMessage>(
            this,
            async (_, _) => await PartyPanel.ReloadClientsAsync(PartyPanel.ClientSearchTerm));

        _messenger.Register<Application.Messages.Clients.ClientUpdatedMessage>(
            this,
            async (_, _) => await PartyPanel.ReloadClientsAsync(PartyPanel.ClientSearchTerm));

        _messenger.Register<Application.Messages.Categories.CategoryCreatedMessage>(
            this,
            async (_, _) => await ProductsPanel.ReloadRootCategoriesAsync());

        _messenger.Register<Application.Messages.Categories.CategoryUpdatedMessage>(
            this,
            async (_, _) => await ProductsPanel.ReloadRootCategoriesAsync());

        _messenger.Register<Application.Messages.Deliveryman.DeliverymanCreatedMessage>(
            this,
            async (_, _) => await ReloadDeliverymenAsync(DeliveryMethodsViewModel.DeliverymanSearchTerm));

        _messenger.Register<Application.Messages.Deliveryman.DeleverymanUpdateMessage>(
            this,
            async (_, _) => await ReloadDeliverymenAsync(DeliveryMethodsViewModel.DeliverymanSearchTerm));

        _messenger.Register<Application.Messages.ShippingCarriers.ShippingCarrierCreatedMessage>(
            this,
            async (_, _) => await ReloadShippingCarriersAsync(DeliveryMethodsViewModel.ShippingCarrierSearchTerm));

        _messenger.Register<Application.Messages.ShippingCarriers.ShippingCarrierUpdatedMessage>(
            this,
            async (_, _) => await ReloadShippingCarriersAsync(DeliveryMethodsViewModel.ShippingCarrierSearchTerm));
    }

    public override bool CanSave =>
        base.CanSave &&
        !PartyPanel.HasErrors &&
        !DeliveryMethodsViewModel.HasErrors &&
        OrderBuilder.Items.Count > 0;

    internal async Task LoadAsync()
    {
        Task productsPanelTask = ProductsPanel.LoadAsync();
        Task partyPanelTask = PartyPanel.LoadAsync();
        var deliverymenTask = _mediator.Send(new Application.Queries.DeliverymanQueries.GetDeliverymenByNameQuery());
        var carriersTask = _mediator.Send(new Application.Queries.ShippingCarriersQueries.GetShippingCarriersByNameQuery());

        await Task.WhenAll(deliverymenTask, carriersTask, productsPanelTask, partyPanelTask);

        DeliveryMethodsViewModel.SetDeliverymen(await deliverymenTask);
        DeliveryMethodsViewModel.SetShippingCarriers(await carriersTask);

        await AfterLoadAsync();
    }

    protected virtual Task AfterLoadAsync() => Task.CompletedTask;

    protected async Task EnsureDeliverymanLoadedAsync(int? deliverymanId)
    {
        if (deliverymanId is null || DeliveryMethodsViewModel.Deliverymen?.Any(deliveryman => deliveryman.Id == deliverymanId) == true)
            return;

        DeliverymanListDto deliveryman = await _mediator.Send(
            new Application.Queries.DeliverymanQueries.GetDeliverymanByIdQuery(deliverymanId.Value));

        if (deliveryman is not null)
        {
            DeliveryMethodsViewModel.SetDeliverymen(
                MergeSelectedItem(DeliveryMethodsViewModel.Deliverymen, deliveryman));
        }
    }

    protected async Task EnsureShippingCarrierLoadedAsync(int? shippingCarrierId)
    {
        if (shippingCarrierId is null || DeliveryMethodsViewModel.ShippingCarriers?.Any(carrier => carrier.Id == shippingCarrierId) == true)
            return;

        ShippingCarrierListDto shippingCarrier = await _mediator.Send(
            new Application.Queries.ShippingCarriersQueries.GetShippingCarrierByIdQuery(shippingCarrierId.Value));

        if (shippingCarrier is not null)
        {
            DeliveryMethodsViewModel.SetShippingCarriers(
                MergeSelectedItem(DeliveryMethodsViewModel.ShippingCarriers, shippingCarrier));
        }
    }

    protected async Task RunWithoutTrackingAsync(Func<Task> action)
    {
        _suspendChangeTracking = true;

        try
        {
            await action();
        }
        finally
        {
            _suspendChangeTracking = false;
        }
    }

    protected void MarkAsChanged()
    {
        if (_suspendChangeTracking)
            return;

        HasChanges = true;
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void RemoveOrderItem(OrderItemViewModel item)
    {
        if (_dialogService.Confirm("Â·  —Ìœ Õ–› Â–« «·⁄‰’— ø"))
            OrderBuilder.RemoveItem(item);
    }

    private void OrderItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (OrderItemViewModel item in e.NewItems)
                item.PropertyChanged += OrderItem_PropertyChanged;
        }

        if (e.OldItems is not null)
        {
            foreach (OrderItemViewModel item in e.OldItems)
                item.PropertyChanged -= OrderItem_PropertyChanged;
        }

        MarkAsChanged();
        SaveCommand.NotifyCanExecuteChanged();
    }

    private void OrderItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        MarkAsChanged();
        SaveCommand.NotifyCanExecuteChanged();
    }

    private async Task ReloadDeliverymenAsync(string searchTerm, System.Threading.CancellationToken cancellationToken = default)
    {
        IEnumerable<DeliverymanListDto> deliverymen = await _mediator.Send(
            new Application.Queries.DeliverymanQueries.GetDeliverymenByNameQuery(searchTerm),
            cancellationToken);

        DeliveryMethodsViewModel.SetDeliverymen(deliverymen);
    }

    private async Task ReloadShippingCarriersAsync(string searchTerm, System.Threading.CancellationToken cancellationToken = default)
    {
        IEnumerable<ShippingCarrierListDto> shippingCarriers = await _mediator.Send(
            new Application.Queries.ShippingCarriersQueries.GetShippingCarriersByNameQuery(searchTerm),
            cancellationToken);

        DeliveryMethodsViewModel.SetShippingCarriers(shippingCarriers);
    }

    protected static IEnumerable<TItem> MergeSelectedItem<TItem>(IEnumerable<TItem> items, TItem selectedItem)
        where TItem : class
    {
        TItem[] results = (items ?? Enumerable.Empty<TItem>()).ToArray();

        if (selectedItem is null || results.Any(item => EqualityComparer<TItem>.Default.Equals(item, selectedItem)))
            return results;

        return new[] { selectedItem }.Concat(results);
    }
}
