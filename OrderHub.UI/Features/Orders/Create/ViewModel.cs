using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Application.Common.Extensions;
using OrderHub.Application.Features.Documents.ProformaInvoices;
using OrderHub.Application.Features.Documents.Quotations;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Application.Features.Orders.Create;
using OrderHub.Application.Features.Orders.Queries;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.UI.Common;
using OrderHub.UI.Features.Orders.Editor;
using OrderHub.UI.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Orders.Create;

internal partial class ViewModel : Editor.ViewModel, IParameterizedViewModel
{
    private readonly IDraftService _draftService;
    private DraftContext _draftContext;
    private readonly IApplicationDirectoriesService _directoriesService;
    private CancellationTokenSource _draftSaveCancellation;
    private readonly IRequestExecutor _requestExecutor;

    public ViewModel(
        IMediator mediator,
        IProductStore productStore,
        ILookupService lookupService,
        IDraftService draftService,
        IRequestExecutor requestExecutor,
        IApplicationDirectoriesService directoriesService)
        : base(
            mediator,
            productStore,
            lookupService)
    {
        _draftService = draftService;
        HasChanges = true;
        _requestExecutor = requestExecutor;

        PartyPanel.PropertyChanged += (_, _) =>
        {
            CreateQuotationCommand.NotifyCanExecuteChanged();
            CreateProformaIncoidCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(CanCreateQuotion));
            OnPropertyChanged(nameof(CanCreateProformaInvoice));
        };

        Items.PropertyChanged += (_, _) =>
        {
            CreateQuotationCommand.NotifyCanExecuteChanged();
            CreateProformaIncoidCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(CanCreateQuotion));
            OnPropertyChanged(nameof(CanCreateProformaInvoice));
        };

        IsDocumentingReady = true;

        _directoriesService = directoriesService;
    }

    public override string Title => "إنشاء طلب جديد";
    public override string ActionName => "إتمام الطلب";

    public Task Initialize(object parameter)
    {
        _draftContext = parameter is Guid draftId
            ? new DraftContext(draftId)
            : new DraftContext(Guid.NewGuid());
        return Task.CompletedTask;
    }

    public bool CanCreateQuotion => Items.Items.Any() && PartyPanel.SelectedClient != null;
    public bool CanCreateProformaInvoice => Items.Items.Any() && PartyPanel.SelectedClient != null;

    [RelayCommand(CanExecute = nameof(CanCreateQuotion))]
    private async Task CreateQuotation()
    {
        CreateQuotationCommand.QuotationData quotation = new CreateQuotationCommand.QuotationData(
            _draftContext.Id,
            PartyPanel.SelectedClient.Name,
            PartyPanel.SelectedClient.PhoneNumber,
            PartyPanel.SelectedClient.Address,
            0.0m,
            Items.Items.Select(x => new CreateQuotationCommand.QuotationItemData(
                x.ProductId,
                x.ProductName,
                x.Price,
                x.Quantity,
                0.0m))
            .ToList());

        string quotationNumber = await _requestExecutor.ExecuteAsync(new CreateQuotationCommand.Command(quotation));
        string path = Path.Combine(_directoriesService.QuotationsDirectory, $"{quotationNumber}.pdf");
        await DialogService.Instance.ShowDialog<Features.Documents.Preview.PreviewDocumentView>(
            "عرض سعر",
            new Features.Documents.Preview.Data(
                path,
                PartyPanel.SelectedClient.Name,
                PartyPanel.SelectedClient.PhoneNumber));
    }

    [RelayCommand(CanExecute = nameof(CanCreateProformaInvoice))]
    private async Task CreateProformaIncoid()
    {
        CreateProformaInvoiceCommand.ProformaInvoiceData data = new CreateProformaInvoiceCommand.ProformaInvoiceData(
            _draftContext.Id,
            PartyPanel.SelectedClient.Name,
            PartyPanel.SelectedClient.PhoneNumber,
            PartyPanel.SelectedClient.Address,
            0.0m,
            Items.Items.Select(x => new CreateProformaInvoiceCommand.ProformaInvoiceItemData(
                x.ProductId,
                x.ProductName,
                x.Price,
                x.Quantity,
                0.0m))
            .ToList());

        string documentNumber = await _requestExecutor.ExecuteAsync(new CreateProformaInvoiceCommand.Command(data));
        string path = Path.Combine(_directoriesService.ProformaInvoicesDirectory, $"{documentNumber}.pdf");

        await DialogService.Instance.ShowDialog<Features.Documents.Preview.PreviewDocumentView>(
            "فاتورة أولية",
            new Features.Documents.Preview.Data(
                path,
                PartyPanel.SelectedClient.Name,
                PartyPanel.SelectedClient.PhoneNumber));
    }

    protected override void MarkAsChanged()
    {
        base.MarkAsChanged();
        ScheduleDraftSave();
    }

    protected override async Task AfterLoadAsync()
    {
        await base.AfterLoadAsync();

        Draft draft = await _draftService.GetAsync(_draftContext);

        if (draft is null)
            return;

        await LoadDraftDependenciesAsync(draft);

        IReadOnlyCollection<GetOrderItemsEditor.OrderItem> items =
            await _mediator.Send(
                new GetOrderItemsEditor.Query(
                    draft.Data.Items.Select(x => x.ProductId).ToList()));

        await RunWithoutTrackingAsync(() =>
        {
            DeliveryMethodsViewModel.DeliverySteps.Clear();

            RestoreOrderHeader(draft);
            BuildOrderItems(draft, items);
            BuildDeliverySteps(draft);

            DeliveryMethodsViewModel.ResetSelectedDeliveryStepsOrder();

            HasChanges = false;

            return Task.CompletedTask;
        });
    }

    protected override async Task Save()
    {
        CancelPendingDraftSave();

        var order = BuildOrder();

        Result<int> result =
            await _mediator.Send(
                new CreateOrderCommand.Command(new CreateOrderCommand.OrderInput(
                    order.ClientId,
                    order.DeliveryMethod,
                    order.DeliveryManId,
                    order.ShippingCarrierId,

                    order.OrderItems.Select(item => new CreateOrderCommand.Item(
                        item.ProductId,
                        item.ProductName,
                        item.Quantity,
                        item.UnitPrice,
                        item.SupplierName,
                        item.SupplierId,
                        item.Properties.Select(property => new CreateOrderCommand.Property(
                            property.PropertyId,
                            property.Value)))),
                    order.DeliverySteps.Select(step => new CreateOrderCommand.DeliveryStep(
                        step.StepOrder,
                        step.DeliveryMethod,
                        step.HandlerId)), 
                    order.PaymentMothodId,
                    _draftContext.Id)));

        if (!result.IsSuccess)
        {
            HandleOrderFailure();
            return;
        }

        await _draftService.DeleteAsync(_draftContext);

        WeakReferenceMessenger.Default.Send(
            new Messages.DraftDeletedMessage(_draftContext.Id));

        HandleOrderSuccess();

        OnRequestClose();
    }

    private async Task LoadDraftDependenciesAsync(Draft draft)
    {
        await EnsureDeliverymanLoadedAsync(
            draft.Data.DeliverymanId);

        await EnsureShippingCarrierLoadedAsync(
            draft.Data.ShippingCarrierId);

        foreach (OrderDraftDeliveryStep step in draft.Data.DeliverySteps)
        {
            if (step.DeliveryMethod == DeliveryMethod.DeliveryMan)
            {
                await EnsureDeliverymanLoadedAsync(
                    step.HandlerId);
            }
            else if (step.DeliveryMethod == DeliveryMethod.ShippingCompany)
            {
                await EnsureShippingCarrierLoadedAsync(
                    step.HandlerId);
            }
        }
    }

    private void RestoreOrderHeader(Draft draft)
    {
        PartyPanel.SelectedClient =
            PartyPanel.Clients?.FirstOrDefault(
                client => client.Id == draft.Data.ClientId);

        PartyPanel.SelectedPaymentMethod =
            PartyPanel.PaymentMethods?.FirstOrDefault(
                method => method.Id == draft.Data.PaymentMethodId);

        DeliveryMethodsViewModel.SelecteddDeliveryMethod =
            Editor.DeliveryMethodsViewModel.DeliveryMethods
                .FirstOrDefault(
                    method => method.Value == draft.Data.DeliveryMethod);

        DeliveryMethodsViewModel.SelectedDeliveryman =
            DeliveryMethodsViewModel.Deliverymen
                ?.FirstOrDefault(
                    deliveryman => deliveryman.Id == draft.Data.DeliverymanId);

        DeliveryMethodsViewModel.SelectedShippingCarrier =
            DeliveryMethodsViewModel.ShippingCarriers
                ?.FirstOrDefault(
                    carrier => carrier.Id == draft.Data.ShippingCarrierId);
    }

    private void BuildOrderItems(
    Draft draft,
    IReadOnlyCollection<GetOrderItemsEditor.OrderItem> items)
    {
        var catalogItemsDict = items
            .DistinctBy(x => x.ProductId)
            .ToDictionary(x => x.ProductId);

        foreach (OrderDraftItem draftItem in draft.Data.Items)
        {
            if (!catalogItemsDict.TryGetValue(
                draftItem.ProductId,
                out GetOrderItemsEditor.OrderItem item))
                continue;

            var suppliers = item.Suppliers
                .Select(supplier => new OrderItemSupplier(
                    supplier.Id,
                    supplier.Name))
                .ToArray();

            OrderItemViewModel orderItem = new()
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                CategoryName = item.CategoryName,
                Price = draftItem.Price,
                Quantity = draftItem.Quantity,
                VAT = draftItem.VAT,

                Suppliers = suppliers,

                Supplier = suppliers.FirstOrDefault(
                    supplier => supplier.Id == draftItem.SupplierId),

                Properties = item.Properties
                    .Select(property => new OrderItemProperty(
                        property.Id,
                        property.Name,
                        property.IsRequired,
                        property.PropertyType,
                        property.Options
                            .Select(option => new OrderItemPropertyOption(
                                option.Id,
                                option.Name))
                            .ToList(),
                        draftItem.Properties
                            .FirstOrDefault(
                                p => p.PropertyId == property.Id)
                            ?.Value))
                    .ToList()
            };

            Items.Add(orderItem);
        }
    }


    private void BuildDeliverySteps(Draft draft)
    {
        foreach (OrderDraftDeliveryStep step in draft.Data.DeliverySteps)
        {
            var handlers = step.DeliveryMethod switch
            {
                DeliveryMethod.DeliveryMan =>
                    DeliveryMethodsViewModel.Deliverymen
                        ?.Select(deliveryman => new Handler(
                            deliveryman.Id,
                            deliveryman.Name)),

                DeliveryMethod.ShippingCompany =>
                    DeliveryMethodsViewModel.ShippingCarriers
                        ?.Select(carrier => new Handler(
                            carrier.Id,
                            carrier.Name)),

                _ => Enumerable.Empty<Handler>()
            };

            DeliveryStepViewModel stepViewModel = new()
            {
                StepOrder = step.StepOrder,
                Method = step.DeliveryMethod,
                Type = step.DeliveryMethod.GetDescription(),
                Handlers = handlers?.ToArray() ?? []
            };

            stepViewModel.SelectedHandler =
                stepViewModel.Handlers.FirstOrDefault(
                    handler => handler.Id == step.HandlerId);

            DeliveryMethodsViewModel.DeliverySteps.Add(
                stepViewModel);
        }
    }

    private void ScheduleDraftSave()
    {
        _draftSaveCancellation?.Cancel();
        _draftSaveCancellation?.Dispose();

        _draftSaveCancellation = new CancellationTokenSource();

        _ = SaveDraftAfterDelayAsync(
            _draftSaveCancellation.Token);
    }

    private async Task SaveDraftAfterDelayAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(
                TimeSpan.FromMilliseconds(500),
                cancellationToken);

            await SaveDraftAsync();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
        }
    }

    private async Task SaveDraftAsync()
    {
        OrderDraftData data = BuildDraftData();

        await _draftService.SaveAsync(
            _draftContext,
            data);

        WeakReferenceMessenger.Default.Send(
            new Messages.DraftSavedMessage(
                _draftContext.Id));
    }

    private OrderDraftData BuildDraftData()
    {
        return new OrderDraftData(
            PartyPanel.SelectedClient?.Id,
            PartyPanel.SelectedClient?.Name,
            PartyPanel.SelectedPaymentMethod?.Id,
            DeliveryMethodsViewModel.SelecteddDeliveryMethod?.Value,
            DeliveryMethodsViewModel.SelectedDeliveryman?.Id,
            DeliveryMethodsViewModel.SelectedShippingCarrier?.Id,

            Items.Items
                .Cast<OrderItemViewModel>()
                .Select(BuildDraftItem)
                .ToList(),

            DeliveryMethodsViewModel.BuildDeliverySteps()
                .Select(step => new OrderDraftDeliveryStep(
                    step.StepOrder,
                    step.DeliveryMethod,
                    step.HandlerId))
                .ToList());
    }

    private static OrderDraftItem BuildDraftItem(
        OrderItemViewModel item)
    {
        return new OrderDraftItem(
            item.ProductId,
            item.ProductName,
            item.CategoryName,
            item.Price,
            item.Quantity,
            item.VAT,
            item.Supplier?.Id,
            item.Supplier?.Name,

            item.Properties
                .Select(property => new OrderDraftProperty(
                    property.Id,
                    property.Value))
                .ToList());
    }

    private void CancelPendingDraftSave()
    {
        _draftSaveCancellation?.Cancel();
        _draftSaveCancellation?.Dispose();
        _draftSaveCancellation = null;
    }

    private void HandleOrderSuccess()
    {
        WeakReferenceMessenger.Default.Send(new Application.Messages.Orders.OrderCreatedMessage());

        PublishSuccessNotification("تم انشاء الطلب بنجاح");
    }

    private void HandleOrderFailure()
    {
        NotificationService.Instance.ShowError("حدث خطأ أثناء إنشاء الطلب");
    }

    private void PublishSuccessNotification(string message)
    {
        NotificationService.Instance.ShowSuccess(message);
    }
}