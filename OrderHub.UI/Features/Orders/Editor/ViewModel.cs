using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Orders.Editor;

internal abstract partial class ViewModel : EditorViewModelBase
{
    protected readonly IMediator _mediator;
    protected readonly IDialogService _dialogService;
    protected readonly IMessenger _messenger;

    private bool _suspendChangeTracking;

    public OrderProductsPanelViewModel ProductsPanel { get; }

    public OrderPartyPanelViewModel PartyPanel { get; }

    public DeliveryMethodsViewModel DeliveryMethodsViewModel { get; }

    public OrderItemsEditorViewModel Items { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private decimal _totalPrice;

    public abstract string ActionName { get; }

    public override bool CanSave =>
        base.CanSave &&
        !PartyPanel.HasErrors &&
        !DeliveryMethodsViewModel.HasErrors &&
        Items.IsValid;

    protected ViewModel(
        IMediator mediator,
        IDialogService dialogService,
        IMessenger messenger,
        IProductStore productStore,
        ILookupService lookupService)
    {
        _mediator = mediator;
        _dialogService = dialogService;
        _messenger = messenger;

        Items = new OrderItemsEditorViewModel();
        Items.PropertyChanged += Items_PropertyChanged;

        ProductsPanel = new OrderProductsPanelViewModel(mediator, productStore);
        PartyPanel = new OrderPartyPanelViewModel(mediator, dialogService, lookupService);
        DeliveryMethodsViewModel = new DeliveryMethodsViewModel(mediator);

        ProductsPanel.ProductSelected += ProductsPanel_ProductSelected;

        _notifyPropertiesNames = [];

        PartyPanel.ErrorsChanged += (_, _) =>
            SaveCommand.NotifyCanExecuteChanged();

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

        _messenger.Register<Application.Messages.Clients.ClientCreatedMessage>(this, async (r, m) =>
        {
            await PartyPanel.LoadAsync();
        });
    }

    private void Items_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        MarkAsChanged();
    }

    internal async Task LoadAsync()
    {
        await ProductsPanel.LoadAsync();
        await PartyPanel.LoadAsync();

        var deliverymen = await _mediator.Send(
            new Application.Queries.DeliverymanQueries.GetDeliverymenByNameQuery());

        var carriers = await _mediator.Send(
            new Application.Queries.ShippingCarriersQueries.GetShippingCarriersByNameQuery());

        DeliveryMethodsViewModel.SetDeliverymen(
            deliverymen.Select(x => new Deliveryman(
                x.Id,
                x.Name,
                x.CityName,
                x.PhoneNumber,
                x.WhatsappGroupName)));

        DeliveryMethodsViewModel.SetShippingCarriers(
            carriers.Select(x => new ShippingCarrier(
                x.Id,
                x.Name,
                x.ShippingCost,
                x.PhoneNumber,
                x.Address)));

        await AfterLoadAsync();
    }

    protected virtual Task AfterLoadAsync() => Task.CompletedTask;

    protected async Task EnsureDeliverymanLoadedAsync(int? deliverymanId)
    {
        if (deliverymanId is null ||
            DeliveryMethodsViewModel.Deliverymen?.Any(d => d.Id == deliverymanId) == true)
        {
            return;
        }

        var deliveryman = await _mediator.Send(
            new Application.Queries.DeliverymanQueries.GetDeliverymanByIdQuery(deliverymanId.Value));

        if (deliveryman is not null)
        {
            DeliveryMethodsViewModel.SetDeliverymen(
                MergeSelectedItem(
                    DeliveryMethodsViewModel.Deliverymen,
                    new Deliveryman(
                        deliveryman.Id,
                        deliveryman.Name,
                        deliveryman.CityName,
                        deliveryman.PhoneNumber,
                        deliveryman.WhatsappGroupName)));
        }
    }

    protected async Task EnsureShippingCarrierLoadedAsync(int? shippingCarrierId)
    {
        if (shippingCarrierId is null ||
            DeliveryMethodsViewModel.ShippingCarriers?.Any(c => c.Id == shippingCarrierId) == true)
        {
            return;
        }

        var shippingCarrier = await _mediator.Send(
            new Application.Queries.ShippingCarriersQueries.GetShippingCarrierByIdQuery(shippingCarrierId.Value));

        if (shippingCarrier is not null)
        {
            DeliveryMethodsViewModel.SetShippingCarriers(
                MergeSelectedItem(
                    DeliveryMethodsViewModel.ShippingCarriers,
                    new ShippingCarrier(
                        shippingCarrier.Id,
                        shippingCarrier.Name,
                        shippingCarrier.ShippingCost,
                        shippingCarrier.PhoneNumber,
                        shippingCarrier.Address)));
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

    protected virtual void MarkAsChanged()
    {
        if (_suspendChangeTracking)
            return;

        HasChanges = true;
    }

    protected static IEnumerable<TItem> MergeSelectedItem<TItem>(
        IEnumerable<TItem> items,
        TItem selectedItem)
        where TItem : class
    {
        TItem[] results = (items ?? Enumerable.Empty<TItem>()).ToArray();

        if (selectedItem is null ||
            results.Any(item => EqualityComparer<TItem>.Default.Equals(item, selectedItem)))
        {
            return results;
        }

        return new[] { selectedItem }.Concat(results);
    }

    [RelayCommand]
    private void RemoveOrderItem(OrderItemViewModel item)
    {
        if (_dialogService.Confirm("åá ÊÑíÏ ÍÐÝ åÐÇ ÇáÚäÕÑ ¿"))
        {
            Items.RemoveItemCommand.Execute(item);
            SaveCommand.NotifyCanExecuteChanged();
        }
    }

    private void ProductsPanel_ProductSelected(ProductSelectedEventArgs e)
    {
        _ = LoadProducDetails(e.Id, e.Price, e.Quantity);
    }

    private async Task LoadProducDetails(int productId, decimal price, int quantity)
    {
        var product = await _mediator.Send(
            new Application.Features.Orders.GetOrderItemEditor.Query(productId));

        decimal finalPrice =
            price > 0
                ? price
                : product.Price;

        OrderItemViewModel orderItemViewModel = new()
        {
            ProductId = product.ProductId,
            ProductName = product.Name,
            CategoryName = product.CategoryName,
            Price = finalPrice,
            Quantity = quantity,

            Suppliers = product.Suppliers.Select(s =>
                new OrderItemSupplier(
                    s.Id,
                    s.Name)),

            Properties = product.Properties.Select(p =>
                new OrderItemProperty(
                    p.Id,
                    p.Name,
                    p.IsRequired,
                    p.PropertyType,
                    p.Options.Select(o =>
                        new OrderItemPropertyOption(
                            o.Id,
                            o.Name))
                    .ToList()))
            .ToList()
        };
        orderItemViewModel.StateChanged += OrderItemViewModel_StateChanged;
        Items.Add(orderItemViewModel);

        SaveCommand.NotifyCanExecuteChanged();
    }

    private void OrderItemViewModel_StateChanged(object sender, EventArgs e)
    {
        SaveCommand.NotifyCanExecuteChanged();
        MarkAsChanged();
    }

    protected OrderDetails.Order BuildOrder()
    {
        return new OrderDetails.Order(
            PartyPanel.SelectedClient.Id,
            DeliveryMethodsViewModel.SelecteddDeliveryMethod.Value,
            DeliveryMethodsViewModel.SelectedDeliveryman?.Id,
            DeliveryMethodsViewModel.SelectedShippingCarrier?.Id,

            Items.Items.Select(item => new OrderDetails.Item(
                item.ProductId,
                item.ProductName,
                item.Quantity,
                item.Price,
                item.Supplier?.Name,
                item.Supplier?.Id,
                item.Properties.Select(property => new OrderDetails.Property(
                    property.Id,
                    property.Value)))),

            DeliveryMethodsViewModel.BuildDeliverySteps().Select(deliveryStep => new OrderDetails.DeliveryStep(
                    deliveryStep.StepOrder,
                    deliveryStep.DeliveryMethod,
                    deliveryStep.HandlerId)),

            PartyPanel.SelectedPaymentMethod.Id);
    }
}

public record ShippingCarrier(
    int Id,
    string Name,
    decimal ShippingCost,
    string PhoneNumber,
    string Address);

public record Deliveryman(
    int Id,
    string Name,
    string CityName,
    string PhoneNumber,
    string WhatsappGroupName = null);