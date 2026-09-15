using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Application.Common.Lookups;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Application.Features.Orders.Queries;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Enums;
using OrderHub.UI.Features.Orders.Index.OrderDetailsPanel;
using OrderHub.UI.Messaging;
using OrderHub.UI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Orders.Index;

internal partial class ViewModel : ObservableObject
{
    private readonly ObservableCollection<OrderViewModel> _orders = [];
    private readonly IRequestExecutor _requestExecutor;

    private CancellationTokenSource _searchCts;

    public ViewModel(
        IMediator mediator,
        IDraftService draftService,
        IRequestExecutor requestExecutor)
    {
        _requestExecutor = requestExecutor;

        OrderDraftsDrawerViewModel =
            new OrderDraftsDrawerViewModel(draftService);

        OrderDetailsPanelViewModel =
            new OrderDetailsPanelViewModel(mediator);

        RegisterMessages();
    }

    public ObservableCollection<OrderViewModel> Orders => _orders;

    public OrderDraftsDrawerViewModel OrderDraftsDrawerViewModel { get; }

    public OrderDetailsPanelViewModel OrderDetailsPanelViewModel { get; }

    // =========================
    // Selection
    // =========================

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedOrder))]
    private OrderViewModel _selectedOrder;

    public bool HasSelectedOrder =>
        SelectedOrder is not null;

    partial void OnSelectedOrderChanged(OrderViewModel value)
    {
        OrderDetailsPanelViewModel.SelectedOrderId = value?.Id;
    }

    // =========================
    // UI State
    // =========================

    [ObservableProperty]
    private bool _isDraftsOrderOpened;

    [ObservableProperty]
    private bool _isOrderDetailsPanelVisible;

    [ObservableProperty]
    private bool _isLoading;

    [RelayCommand]
    private async Task ShowDraftsOrders()
    {
        IsDraftsOrderOpened = !IsDraftsOrderOpened;

        if (IsDraftsOrderOpened)
            await OrderDraftsDrawerViewModel.LoadAsync();
    }

    [RelayCommand]
    private void SwitchOrderDetailsPanel()
    {
        IsOrderDetailsPanelVisible =
            !IsOrderDetailsPanelVisible;
    }

    // =========================
    // Filters
    // =========================

    [ObservableProperty]
    private IEnumerable<PaymentMethod> _paymentMethods;

    [ObservableProperty]
    private PaymentMethod _selectedPaymentMethod;

    [ObservableProperty]
    private EnumItem<OrderStatus> _selectedOrderStatus;

    [ObservableProperty]
    private string _searchTerm;

    [ObservableProperty]
    private DateTime? _fromDate = DateTime.Today;

    [ObservableProperty]
    private DateTime? _toDate = DateTime.Today;

    public IEnumerable<PaymentMethod> PaymentMethodFilters =>
        new[]
        {
            new PaymentMethod(
                0,
                "الكل",
                string.Empty,
                true)
        }
        .Concat(
            PaymentMethods ??
            Enumerable.Empty<PaymentMethod>());

    public IEnumerable<EnumItem<OrderStatus>> OrderStatusFilters =>
        new[]
        {
            new EnumItem<OrderStatus>(
                default,
                "الكل")
        }
        .Concat(EnumItems.For<OrderStatus>());

    // =========================
    // Pagination
    // =========================

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _pageSize = 20;

    [ObservableProperty]
    private int _totalPages;

    [ObservableProperty]
    private int _totalCount;

    public bool CanGoPreviousPage =>
        CurrentPage > 1;

    public bool CanGoNextPage =>
        CurrentPage < TotalPages;

    public string PaginationSummary =>
        TotalCount == 0
            ? "لا توجد طلبات"
            : $"الصفحة {CurrentPage} من {TotalPages} - إجمالي {TotalCount}";

    // =========================
    // Loading
    // =========================

    [RelayCommand]
    private async Task Load()
    {
        await OrderDraftsDrawerViewModel.LoadAsync();

        try
        {
            IsLoading = true;

            PaymentMethods =
                (await _requestExecutor.ExecuteAsync(
                    new Application.Queries.PaymentMothodQueries
                        .GetPaymentMethodListQuery()))
                .Select(method =>
                    new PaymentMethod(
                        method.Id,
                        method.DisplayName,
                        method.Description,
                        method.IsActive));

            OnPropertyChanged(nameof(PaymentMethodFilters));

            SelectedPaymentMethod ??=
                PaymentMethodFilters.FirstOrDefault();

            SelectedOrderStatus ??=
                OrderStatusFilters.FirstOrDefault();
        }
        catch (Exception ex)
        {
            NotificationService.Instance.Show(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private Task ReloadAsync() => Load();

    // =========================
    // Filter Changes
    // =========================

    async partial void OnSearchTermChanged(
        string oldValue,
        string newValue)
    {
        await RefreshWithPagingAsync(300);
    }

    async partial void OnFromDateChanged(
        DateTime? oldValue,
        DateTime? newValue)
    {
        await RefreshWithPagingAsync();
    }

    async partial void OnToDateChanged(
        DateTime? oldValue,
        DateTime? newValue)
    {
        await RefreshWithPagingAsync();
    }

    async partial void OnSelectedPaymentMethodChanged(
        PaymentMethod value)
    {
        await RefreshWithPagingAsync();
    }

    async partial void OnSelectedOrderStatusChanged(
        EnumItem<OrderStatus> oldValue,
        EnumItem<OrderStatus> newValue)
    {
        await RefreshWithPagingAsync();
    }

    // =========================
    // Pagination State
    // =========================

    partial void OnCurrentPageChanged(
        int oldValue,
        int newValue)
    {
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();

        OnPropertyChanged(nameof(PaginationSummary));
        OnPropertyChanged(nameof(CanGoPreviousPage));
        OnPropertyChanged(nameof(CanGoNextPage));
    }

    partial void OnTotalPagesChanged(
        int oldValue,
        int newValue)
    {
        NextPageCommand.NotifyCanExecuteChanged();

        OnPropertyChanged(nameof(PaginationSummary));
        OnPropertyChanged(nameof(CanGoNextPage));
    }

    partial void OnTotalCountChanged(
        int oldValue,
        int newValue)
    {
        OnPropertyChanged(nameof(PaginationSummary));
    }

    // =========================
    // Pagination Commands
    // =========================

    [RelayCommand(CanExecute = nameof(CanGoPreviousPage))]
    private async Task PreviousPage()
    {
        if (!CanGoPreviousPage)
            return;

        CurrentPage--;

        await LoadOrdersPageAsync();
    }

    [RelayCommand(CanExecute = nameof(CanGoNextPage))]
    private async Task NextPage()
    {
        if (!CanGoNextPage)
            return;

        CurrentPage++;

        await LoadOrdersPageAsync();
    }

    // =========================
    // Date Commands
    // =========================

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
        if (!int.TryParse(day, out int dayNumber))
            return;

        if (FromDate is null || ToDate is null)
            return;

        FromDate = FromDate.Value.AddDays(dayNumber);
        ToDate = ToDate.Value.AddDays(dayNumber);

        await RefreshWithPagingAsync();
    }

    [RelayCommand]
    private async Task SetCurrentWeek()
    {
        DateTime today = DateTime.Today;

        int diff =
            ((int)today.DayOfWeek + 6) % 7;

        DateTime startOfWeek =
            today.AddDays(-diff);

        FromDate = startOfWeek;
        ToDate = startOfWeek.AddDays(6);

        await RefreshWithPagingAsync();
    }

    [RelayCommand]
    private async Task SetCurrentMonth()
    {
        DateTime today = DateTime.Today;

        DateTime startOfMonth =
            new(today.Year, today.Month, 1);

        DateTime endOfMonth =
            startOfMonth
                .AddMonths(1)
                .AddDays(-1);

        FromDate = startOfMonth;
        ToDate = endOfMonth;

        await RefreshWithPagingAsync();
    }

    // =========================
    // Data
    // =========================

    private async Task RefreshWithPagingAsync(
        int delay = 100)
    {
        CurrentPage = 1;

        _searchCts?.Cancel();
        _searchCts?.Dispose();

        _searchCts =
            new CancellationTokenSource();

        try
        {
            await Task.Delay(
                delay,
                _searchCts.Token);

            await LoadOrdersPageAsync(
                _searchCts.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task LoadOrdersPageAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            IsLoading = true;

            int? paymentMethodId =
                SelectedPaymentMethod is null ||
                SelectedPaymentMethod.Id == 0
                    ? null
                    : SelectedPaymentMethod.Id;

            OrderStatus? orderStatus =
                SelectedOrderStatus is null ||
                SelectedOrderStatus.DisplayName == "الكل"
                    ? null
                    : SelectedOrderStatus.Value;

            var pagedOrders =
                await _requestExecutor.ExecuteAsync(
                    new GetOrderPaged.Query(
                        CurrentPage,
                        PageSize,
                        SearchTerm,
                        FromDate,
                        ToDate,
                        paymentMethodId,
                        orderStatus),
                    cancellationToken);

            TotalPages = pagedOrders.TotalPages;
            TotalCount = pagedOrders.TotalCount;
            CurrentPage = pagedOrders.PageNumber;

            UpdateOrders(
                pagedOrders.Items.Select(Map));
        }
        catch (Exception ex)
            when (ex is not OperationCanceledException)
        {
            NotificationService.Instance
                .ShowError(ex.Message);
        }
        finally
        {
            IsLoading = false;

            PreviousPageCommand
                .NotifyCanExecuteChanged();

            NextPageCommand
                .NotifyCanExecuteChanged();

            OnPropertyChanged(
                nameof(PaginationSummary));
        }
    }

    private void UpdateOrders(
        IEnumerable<OrderViewModel> orders)
    {
        _orders.Clear();

        foreach (OrderViewModel order in orders)
            _orders.Add(order);
    }

    private OrderViewModel Map(
        GetOrderPaged.Order order)
    {
        return new OrderViewModel(
            _requestExecutor,
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
            order.IsDeliverymanMessageSent);
    }

    // =========================
    // Messages
    // =========================

    private void RegisterMessages()
    {
        WeakReferenceMessenger.Default
            .Register<OrdersMessages.OrderCreatedMessage>(
                this,
                async (_, _) => await ReloadAsync());

        WeakReferenceMessenger.Default
            .Register<OrdersMessages.OrderUpdatedMessage>(
                this,
                async (_, _) => await ReloadAsync());

        WeakReferenceMessenger.Default
            .Register<OrdersMessages.OrderStatusChangedMessage>(
                this,
                async (_, _) => await ReloadAsync());

        WeakReferenceMessenger.Default
            .Register<OrdersMessages.OrderPaymentMethodChangedMessage>(
                this,
                async (_, _) => await ReloadAsync());

        WeakReferenceMessenger.Default
            .Register<OrdersMessages.OrderDeletedMessage>(
                this,
                async (_, _) => await ReloadAsync());

        WeakReferenceMessenger.Default.Register<
            Application.Messages.OutboxMessages
                .MessageStatusChangedMessage>(
            this,
            (_, message) =>
            {
                OrderViewModel order =
                    Orders.FirstOrDefault(
                        item =>
                            item.Id == message.OrderId);

                order?.UpdateRecipientStatus(
                    message.RecipientType,
                    message.NewStatus ==
                    OutboxMessageStatus.Sent);
            });

        WeakReferenceMessenger.Default.Register<
            ViewModel,
            OrdersMessages.OrderDraftSavedMessage>(
            this,
            (_, _) =>
            {
                OnPropertyChanged(
                    nameof(
                        OrderDraftsDrawerViewModel
                            .Drafts.Count));
            });

        WeakReferenceMessenger.Default.Register<
            ViewModel,
            OrdersMessages.OrderDraftDeletedMessage>(
            this,
            (_, _) =>
            {
                OnPropertyChanged(
                    nameof(
                        OrderDraftsDrawerViewModel
                            .Drafts.Count));
            });
    }
}
