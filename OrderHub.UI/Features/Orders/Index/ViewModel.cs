using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Features.Orders.Queries;
using OrderHub.UI.Common;
using OrderHub.UI.Features.Orders.Index.OrderDetailsPanel;
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
    private readonly IRequestExecutor _requestExecutor;
    private readonly ObservableCollection<OrderViewModel> _orders = [];

    private CancellationTokenSource _filterCts;
    private bool _useFilter;

    public ViewModel(
        IMediator mediator,
        IRequestExecutor requestExecutor)
    {
        _requestExecutor = requestExecutor;

        OrderDraftsDrawerViewModel =
            new OrderDraftsDrawerViewModel(requestExecutor);

        OrderDetailsPanelViewModel =
            new OrderDetailsPanelViewModel(mediator);

        Filter.FilterChanged += OnFilterChanged;
        Pagination.PageChanged += OnPageChanged;

        RegisterMessages();
    }

    // =========================
    // Child ViewModels
    // =========================

    public OrdersFilter Filter { get; } = new();

    public PaginationViewModel Pagination { get; } = new();

    public OrderDraftsDrawerViewModel OrderDraftsDrawerViewModel { get; }

    public OrderDetailsPanelViewModel OrderDetailsPanelViewModel { get; }

    // =========================
    // Orders
    // =========================

    public ObservableCollection<OrderViewModel> Orders =>
        _orders;

    // =========================
    // Selection
    // =========================

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedOrder))]
    private OrderViewModel _selectedOrder;

    public bool HasSelectedOrder =>
        SelectedOrder is not null;

    partial void OnSelectedOrderChanged(
        OrderViewModel value)
    {
        OrderDetailsPanelViewModel.SelectedOrderId =
            value?.Id;
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

    // =========================
    // Commands
    // =========================

    [RelayCommand]
    private async Task Load()
    {
        CancelFilterRequest();

        _useFilter = false;

        Pagination.Reset();

        await LoadFilterAsync();
        await LoadOrdersAsync();
    }

    [RelayCommand]
    private async Task ShowDraftsOrders()
    {
        IsDraftsOrderOpened =
            !IsDraftsOrderOpened;

        if (IsDraftsOrderOpened)
        {
            await OrderDraftsDrawerViewModel
                .LoadAsync();
        }
    }

    [RelayCommand]
    private void SwitchOrderDetailsPanel()
    {
        IsOrderDetailsPanelVisible =
            !IsOrderDetailsPanelVisible;
    }

    // =========================
    // Filter
    // =========================

    private async void OnFilterChanged(
        object sender,
        EventArgs e)
    {
        CancelFilterRequest();

        var cts =
            new CancellationTokenSource();

        _filterCts = cts;

        try
        {
            await Task.Delay(
                300,
                cts.Token);

            _useFilter = true;

            Pagination.Reset();

            await LoadOrdersAsync(
                cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task LoadFilterAsync()
    {
        var paymentMethods =
            (await _requestExecutor.ExecuteAsync(
                new Application.Queries
                    .PaymentMothodQueries
                    .GetPaymentMethodListQuery()))
            .Select(method =>
                new PaymentMethod(
                    method.Id,
                    method.DisplayName,
                    method.Description,
                    method.IsActive))
            .ToList();

        Filter.Initialize(
            paymentMethods);
    }

    // =========================
    // Pagination
    // =========================

    private async void OnPageChanged(
        object sender,
        EventArgs e)
    {
        CancelFilterRequest();

        await LoadOrdersAsync();
    }

    // =========================
    // Loading
    // =========================

    private async Task LoadOrdersAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            IsLoading = true;

            var pagedOrders =
                await _requestExecutor.ExecuteAsync(
                    new GetOrderPaged.Query(
                        Pagination.CurrentPage,
                        Pagination.PageSize,

                        _useFilter
                            ? Filter.SearchTerm
                            : null,

                        _useFilter
                            ? Filter.FromDate
                            : null,

                        _useFilter
                            ? Filter.ToDate
                            : null,

                        _useFilter
                            ? Filter.PaymentMethodId
                            : null,

                        _useFilter
                            ? Filter.OrderStatus
                            : null),
                    cancellationToken);

            Pagination.Update(
                pagedOrders.PageNumber,
                pagedOrders.TotalPages,
                pagedOrders.TotalCount);

            UpdateOrders(
                pagedOrders.Items.Select(
                    order =>
                        OrderViewModelMapper.Map(
                            order,
                            _requestExecutor)));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            NotificationService.Instance
                .ShowError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private Task ReloadAsync()
    {
        return LoadOrdersAsync();
    }

    // =========================
    // Helpers
    // =========================

    private void UpdateOrders(
        IEnumerable<OrderViewModel> orders)
    {
        _orders.Clear();

        foreach (OrderViewModel order in orders)
        {
            _orders.Add(order);
        }
    }

    private void CancelFilterRequest()
    {
        _filterCts?.Cancel();
        _filterCts?.Dispose();

        _filterCts = null;
    }
}
