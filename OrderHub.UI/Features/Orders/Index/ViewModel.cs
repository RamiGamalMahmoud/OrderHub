using AutoMapper.Internal;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Application.Common.Lookups;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Application.Features.Orders.Queries;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.UI.Common;
using OrderHub.UI.Features.Orders.Index.OrderDetailsPanel;
using OrderHub.UI.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Orders.Index;

internal partial class ViewModel : IndexViewModelBase<OrderViewModel>
{
    private readonly IDialogService _dialogService;
    public OrderDraftsDrawerViewModel OrderDraftsDrawerViewModel { get; }

    [ObservableProperty]
    private bool _isDraftsOrderOpened;

    [RelayCommand]
    private async Task ShowDraftsOrders()
    {
        IsDraftsOrderOpened = !IsDraftsOrderOpened;
        if(IsDraftsOrderOpened)
            await OrderDraftsDrawerViewModel.LoadAsync();
    }

    public OrderDetailsPanelViewModel OrderDetailsPanelViewModel { get; }

    private readonly ObservableCollection<OrderViewModel> _orders = new();
    public ObservableCollection<OrderViewModel> Orders => _orders;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedOrder))]
    private OrderViewModel _selectedOrder;

    partial void OnSelectedOrderChanged(OrderViewModel value)
    {
        OrderDetailsPanelViewModel.SelectedOrderId = value?.Id;
    }

    public bool HasSelectedOrder => SelectedOrder is not null;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private IEnumerable<PaymentMethod> _paymentMethods;

    [ObservableProperty]
    private PaymentMethod _selectedPaymentMethod;

    public IEnumerable<PaymentMethod> PaymentMethodFilters =>
        new[] { new PaymentMethod(0, "الكل", string.Empty, true) }
        .Concat(PaymentMethods ?? Enumerable.Empty<PaymentMethod>());

    public IEnumerable<EnumItem<OrderStatus>> OrderStatusFilters =>
        new[] { new EnumItem<OrderStatus>(default, "الكل") }
        .Concat(EnumItems.For<OrderStatus>());

    [ObservableProperty]
    private string _searchTerm;

    [ObservableProperty]
    private DateTime? _fromDate = DateTime.Today;

    [ObservableProperty]
    private DateTime? _toDate = DateTime.Today;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _pageSize = 20;

    [ObservableProperty]
    private int _totalPages;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private EnumItem<OrderStatus> _selectedOrderStatus;

    private CancellationTokenSource _searchCts;

    public bool CanGoPreviousPage => CurrentPage > 1;
    public bool CanGoNextPage => CurrentPage < TotalPages;
    public string PaginationSummary => TotalCount == 0
        ? "لا توجد طلبات"
        : $"الصفحة {CurrentPage} من {TotalPages} - إجمالي {TotalCount}";

    public ViewModel(
        IMediator mediator,
        IMessenger messenger,
        IDraftService draftService,
        IDialogService dialogService)
        : base(mediator, messenger)
    {
        _dialogService = dialogService;
        OrderDraftsDrawerViewModel = new OrderDraftsDrawerViewModel(draftService, dialogService, messenger);

        OrderDetailsPanelViewModel = new OrderDetailsPanelViewModel(mediator, dialogService);

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

        _messenger.Register<ViewModel, Messages.DraftSavedMessage>(this, (r, m) =>
        {
            OnPropertyChanged(nameof(OrderDraftsDrawerViewModel.Drafts.Count));
        });

        _messenger.Register<ViewModel, Messages.DraftDeletedMessage>(this, (r, m) =>
        {
            OnPropertyChanged(nameof(OrderDraftsDrawerViewModel.Drafts.Count));
        });
    }

    protected override async Task LoadAsync()
    {
        try
        {
            IsLoading = true;

            PaymentMethods = (await _mediator.Send(
                new Application.Queries.PaymentMothodQueries.GetPaymentMethodListQuery()))
                .Select(method => new PaymentMethod(
                    method.Id,
                    method.DisplayName,
                    method.Description,
                    method.IsActive));

            OnPropertyChanged(nameof(PaymentMethodFilters));
            SelectedPaymentMethod ??= PaymentMethodFilters.FirstOrDefault();
            SelectedOrderStatus ??= OrderStatusFilters.FirstOrDefault();

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
        //await Task.Delay(300);
        await RefreshWithPagingAsync(300);
    }

    async partial void OnFromDateChanged(DateTime? oldValue, DateTime? newValue) => await RefreshWithPagingAsync();

    async partial void OnToDateChanged(DateTime? oldValue, DateTime? newValue) => await RefreshWithPagingAsync();

    async partial void OnSelectedPaymentMethodChanged(PaymentMethod value) => await RefreshWithPagingAsync();

    async partial void OnSelectedOrderStatusChanged(EnumItem<OrderStatus> oldValue, EnumItem<OrderStatus> newValue) => await RefreshWithPagingAsync();

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
        _dialogService.ShowDialog<Edit.View>(model.Id);
        return Task.CompletedTask;
    }

    protected override async Task DeleteAsync(OrderViewModel model)
    {
        if (!_dialogService.Confirm($"هل تريد حذف الطلب ({model.OrderNumber})؟"))
        {
            return;
        }

        Result result = await _mediator.Send(
            new Application.Features.Orders.Delete.DeleteOrder.Command(model.Id));

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

    private OrderViewModel Map(GetOrderPaged.Order order) => new(
        _mediator,
        _messenger,
        order.Id,
        order.OrderNumber,
        order.ClientName,
        order.ClientPhoneNumber,
        order.ItemsCount,
        order.Total,
        order.CreatedAt,

        new PaymentMethod(
            order.PaymentMethod.Id,
            order.PaymentMethod.DisplayName,
            order.PaymentMethod.Description,
            order.PaymentMethod.IsActive),

        order.EnumItem,
        order.HasClientRecipient,
        order.HasSupplierRecipient,
        order.HasShippingCarrierRecipient,
        order.HasDeliverymanRecipient,
        order.IsClientMessageSent,
        order.IsSupplierMessageSent,
        order.IsShippingCarrierMessageSent,
        order.IsDeliverymanMessageSent
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

    [RelayCommand]
    private async Task SetToday()
    {
        FromDate = DateTime.Today;
        ToDate = DateTime.Today;
        await RefreshWithPagingAsync();
    }

    [RelayCommand]
    private async Task SetDay(string day)
    {
        _ = int.TryParse(day, out int dayNumber);
        FromDate = FromDate.Value.AddDays(dayNumber);
        ToDate = ToDate.Value.AddDays(dayNumber);
        await RefreshWithPagingAsync();
    }

    [RelayCommand]
    private async Task SetCurrentWeek()
    {
        DateTime today = DateTime.Today;
        int diff = ((int)today.DayOfWeek + 6) % 7;
        DateTime startOfWeek = today.AddDays(-diff);

        FromDate = startOfWeek;
        ToDate = startOfWeek.AddDays(6);
        await RefreshWithPagingAsync();
    }

    [RelayCommand]
    private async Task SetCurrentMonth()
    {
        DateTime today = DateTime.Today;
        DateTime startOfMonth = new(today.Year, today.Month, 1);
        DateTime endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        FromDate = startOfMonth;
        ToDate = endOfMonth;
        await RefreshWithPagingAsync();
    }

    private async Task RefreshWithPagingAsync(int delay = 100)
    {
        CurrentPage = 1;

        _searchCts?.Cancel();
        _searchCts?.Dispose();

        _searchCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(delay, _searchCts.Token);
            await LoadOrdersPageAsync(_searchCts.Token);
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

            int? selectedPaymentMethodId = SelectedPaymentMethod is null || SelectedPaymentMethod.Id == 0
                ? null
                : SelectedPaymentMethod.Id;
            OrderStatus? selectedOrderStatus = SelectedOrderStatus is null || SelectedOrderStatus.DisplayName == "الكل"
                ? null
                : SelectedOrderStatus.Value;

            var pagedOrders = await _mediator.Send(
                new GetOrderPaged.Query(
                    CurrentPage,
                    PageSize,
                    SearchTerm,
                    FromDate,
                    ToDate,
                    selectedPaymentMethodId,
                    selectedOrderStatus),
                cancellationToken);

            TotalPages = pagedOrders.TotalPages;
            TotalCount = pagedOrders.TotalCount;
            CurrentPage = pagedOrders.PageNumber;

            UpdateOrders(pagedOrders.Items.Select(Map));
            //OnPropertyChanged(nameof(PaymentMethodFilters));
            //OnPropertyChanged(nameof(OrderStatusFilters));
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
