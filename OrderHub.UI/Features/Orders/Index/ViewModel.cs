using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using OrderHub.UI.Stores.Markers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Common;
using static OrderHub.Application.DTOs.OrderDtos;
using static OrderHub.Application.DTOs.PaymentMothodsDtos;

namespace OrderHub.UI.Features.Orders.Index;

internal partial class ViewModel : IndexViewModelBase<OrderViewModel>
{
    private readonly IDialogService _dialogService;
    private readonly ISelectionStore<IOrderMarker, int> _selectionStore;

    private readonly ObservableCollection<OrderViewModel> _orders = new();
    public ObservableCollection<OrderViewModel> Orders => _orders;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private IEnumerable<PaymentMethodListDto> _paymentMethods;

    [ObservableProperty]
    private string _searchTerm;

    [ObservableProperty]
    private DateTime? _fromDate = DateTime.Today;

    [ObservableProperty]
    private DateTime? _toDate = DateTime.Today;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _pageSize = 4;

    [ObservableProperty]
    private int _totalPages;

    [ObservableProperty]
    private int _totalCount;

    private CancellationTokenSource _searchCts;

    public bool CanGoPreviousPage => CurrentPage > 1;
    public bool CanGoNextPage => CurrentPage < TotalPages;
    public string PaginationSummary => TotalCount == 0
        ? "لا توجد طلبات"
        : $"الصفحة {CurrentPage} من {TotalPages} - إجمالي {TotalCount}";

    public ViewModel(
        IMediator mediator,
        IMessenger messenger,
        IDialogService dialogService,
        ISelectionStore<IOrderMarker, int> selectionStore)
        : base(mediator, messenger)
    {
        _dialogService = dialogService;
        _selectionStore = selectionStore;

        messenger.Register<Application.Messages.Orders.OrderCreatedMessage>(
            this,
            async (_, _) => await ReloadAsync());

        messenger.Register<Application.Messages.Orders.OrderUpdatedMessage>(
            this,
            async (_, _) => await ReloadAsync());

        messenger.Register<Application.Messages.Orders.OrderDeletedMessage>(
            this,
            async (_, _) => await ReloadAsync());

        messenger.Register<Application.Messages.OutboxMessages.MessageStatusChangedMessage>(
            this,
            (_, m) =>
            {
                OrderViewModel order = Orders.FirstOrDefault(item => item.Id == m.OrderId);
                order?.UpdateRecipientStatus(m.RecipientType, m.NewStatus == Domain.Enums.OutboxMessageStatus.Sent);
            });
    }

    // =========================
    // 🔄 Lifecycle
    // =========================

    protected override async Task LoadAsync()
    {
        try
        {
            IsLoading = true;

            PaymentMethods = await _mediator.Send(
                new Application.Queries.PaymentMothodQueries.GetPaymentMethodListQuery());

            await LoadOrdersPageAsync();
        }
        catch (Exception ex)
        {
            await _mediator.Publish(
                new Application.Notifications.AppliationNotification(ex.Message));
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected override Task ReloadAsync() => LoadAsync();

    // =========================
    // 🔍 Debounced Search
    // =========================

    async partial void OnSearchTermChanged(string oldValue, string newValue)
    {
        await RefreshWithPagingAsync();
    }

    async partial void OnFromDateChanged(DateTime? oldValue, DateTime? newValue) => await RefreshWithPagingAsync();

    async partial void OnToDateChanged(DateTime? oldValue, DateTime? newValue) => await RefreshWithPagingAsync();

    partial void OnCurrentPageChanged(int oldValue, int newValue)
    {
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(PaginationSummary));
        OnPropertyChanged(nameof(CanGoPreviousPage));
        OnPropertyChanged(nameof(CanGoNextPage));
    }

    partial void OnTotalPagesChanged(int oldValue, int newValue)
    {
        NextPageCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(PaginationSummary));
        OnPropertyChanged(nameof(CanGoNextPage));
    }

    partial void OnTotalCountChanged(int oldValue, int newValue) => OnPropertyChanged(nameof(PaginationSummary));


    protected override Task ShowCreateAsync()
    {
        _dialogService.ShowDialog<Create.View>();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task BroadcastOrder(OrderViewModel model)
    {
        await BroadcastOrderAsync(model, null);
    }

    [RelayCommand]
    private Task BroadcastClient(OrderViewModel model) => BroadcastOrderAsync(model, RecipientType.Client);

    [RelayCommand]
    private Task BroadcastSupplier(OrderViewModel model) => BroadcastOrderAsync(model, RecipientType.Supplier);

    [RelayCommand]
    private Task BroadcastShippingCarrier(OrderViewModel model) => BroadcastOrderAsync(model, RecipientType.ShippingCarrier);

    [RelayCommand]
    private Task BroadcastDeliveryman(OrderViewModel model) => BroadcastOrderAsync(model, RecipientType.Deliveryman);

    private async Task BroadcastOrderAsync(OrderViewModel model, RecipientType? recipientType)
    {
        Result result = await _mediator.Send(
            new Application.Commands.OrderCommands.BroadcastOrderStatusCommand(model.Id, recipientType));

        if (result.IsSuccess)
        {
            return;
        }

        await _mediator.Publish(
            new Application.Notifications.AppliationNotification(result.ErrorMessage));
    }

    [RelayCommand]
    private async Task BroadcastAllOrders()
    {
        try
        {
            await Task.WhenAll(
                Orders.Select(order =>
                    _mediator.Send(
                        new Application.Commands.OrderCommands.BroadcastOrderStatusCommand(order.Id)))
            );
        }
        catch (Exception ex)
        {
            await _mediator.Publish(
                new Application.Notifications.AppliationNotification(ex.Message));
        }
    }

    protected override Task ShowEditAsync(OrderViewModel model)
    {
        _selectionStore.Id = model.Id;
        _dialogService.ShowDialog<Edit.View>();
        return Task.CompletedTask;
    }

    protected override async Task DeleteAsync(OrderViewModel model)
    {
        if (!_dialogService.Confirm($"هل تريد حذف الطلب ({model.OrderNumber})؟"))
        {
            return;
        }

        Result result = await _mediator.Send(
            new Application.Commands.OrderCommands.DeleteOrderCommand(model.Id));

        if (!result.IsSuccess)
        {
            await _mediator.Publish(
                new Application.Notifications.ErrorNotification(result.ErrorMessage));
            return;
        }

        _messenger.Send(new Application.Messages.Orders.OrderDeletedMessage(model.Id));
        await _mediator.Publish(
            new Application.Notifications.SuccessNotification("تم حذف الطلب بنجاح."));
    }


    private void UpdateOrders(IEnumerable<OrderViewModel> newOrders)
    {
        _orders.Clear();
        foreach (var order in newOrders)
        {
            _orders.Add(order);
        }
    }

    private OrderViewModel Map(OrderListDto o) => new(
        _mediator,
        o.Id,
        o.OrderNumber,
        o.ClientName,
        o.ClientPhoneNumber,
        o.ItemsCount,
        o.Total,
        o.CreatedAt,
        o.PaymentMethod,
        o.EnumItem,
        o.HasClientRecipient,
        o.HasSupplierRecipient,
        o.HasShippingCarrierRecipient,
        o.HasDeliverymanRecipient,
        o.IsClientMessageSent,
        o.IsSupplierMessageSent,
        o.IsShippingCarrierMessageSent,
        o.IsDeliverymanMessageSent
    );

    [RelayCommand(CanExecute = nameof(CanGoPreviousPage))]
    private async Task PreviousPage()
    {
        if (CurrentPage <= 1)
        {
            return;
        }

        CurrentPage--;
        await LoadOrdersPageAsync();
    }

    [RelayCommand(CanExecute = nameof(CanGoNextPage))]
    private async Task NextPage()
    {
        if (CurrentPage >= TotalPages)
        {
            return;
        }

        CurrentPage++;
        await LoadOrdersPageAsync();
    }

    private async Task RefreshWithPagingAsync()
    {
        CurrentPage = 1;

        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        CancellationToken token = _searchCts.Token;

        try
        {
            await LoadOrdersPageAsync(token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task LoadOrdersPageAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IsLoading = true;

            PagedResult<OrderListDto> pagedOrders = await _mediator.Send(
                new Application.Queries.OrderQueries.GetOrdersPagedQuery(
                    CurrentPage,
                    PageSize,
                    SearchTerm,
                    FromDate,
                    ToDate),
                cancellationToken);

            TotalPages = pagedOrders.TotalPages;
            TotalCount = pagedOrders.TotalCount;
            CurrentPage = pagedOrders.PageNumber;

            UpdateOrders(pagedOrders.Items.Select(Map));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await _mediator.Publish(
                new Application.Notifications.AppliationNotification(ex.Message));
        }
        finally
        {
            IsLoading = false;
            PreviousPageCommand.NotifyCanExecuteChanged();
            NextPageCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(PaginationSummary));
        }
    }
}
