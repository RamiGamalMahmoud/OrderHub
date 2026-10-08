using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrderHub.Application.Common.Lookups;
using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderHub.UI.Features.Orders.Index;

internal partial class OrdersFilter : ObservableObject
{
    [ObservableProperty]
    private string _searchTerm;

    [ObservableProperty]
    private DateTime? _fromDate = DateTime.Today;

    [ObservableProperty]
    private DateTime? _toDate = DateTime.Today;

    [ObservableProperty]
    private PaymentMethod _selectedPaymentMethod;

    [ObservableProperty]
    private EnumItem<OrderStatus> _selectedOrderStatus;

    [ObservableProperty]
    private IEnumerable<PaymentMethod> _paymentMethods;

    public event EventHandler FilterChanged;

    private bool _suppressChanged;

    public void Initialize(
    IEnumerable<PaymentMethod> paymentMethods)
    {
        _suppressChanged = true;

        try
        {
            PaymentMethods = paymentMethods.ToList();

            SelectedPaymentMethod =
                PaymentMethodFilters.FirstOrDefault();

            SelectedOrderStatus =
                OrderStatusFilters.FirstOrDefault();
        }
        finally
        {
            _suppressChanged = false;
        }
    }

    private void OnFilterChanged()
    {
        if (!_suppressChanged)
            FilterChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Update(Action action)
    {
        _suppressChanged = true;

        try
        {
            action();
        }
        finally
        {
            _suppressChanged = false;
        }

        OnFilterChanged();
    }

    public IEnumerable<PaymentMethod> PaymentMethodFilters =>
        new[]
        {
            new PaymentMethod(0, "الكل", string.Empty, true)
        }
        .Concat(PaymentMethods ?? []);

    public IEnumerable<EnumItem<OrderStatus>> OrderStatusFilters =>
        new[]
        {
            new EnumItem<OrderStatus>(default, "الكل")
        }
        .Concat(EnumItems.For<OrderStatus>());

    public int? PaymentMethodId =>
        SelectedPaymentMethod is null ||
        SelectedPaymentMethod.Id == 0
            ? null
            : SelectedPaymentMethod.Id;

    public OrderStatus? OrderStatus =>
        SelectedOrderStatus is null ||
        SelectedOrderStatus.DisplayName == "الكل"
            ? null
            : SelectedOrderStatus.Value;

    [RelayCommand]
    public void SetToday()
    {
        Update(() =>
        {
            FromDate = DateTime.Today;
            ToDate = DateTime.Today;
        });
    }

    [RelayCommand]
    public void MoveDay(string daysText)
    {
        if (!int.TryParse(daysText, out int days))
            return;

        if (FromDate is null || ToDate is null)
            return;
        Update(() =>
        {

            FromDate = FromDate.Value.AddDays(days);
            ToDate = ToDate.Value.AddDays(days);
        });
    }

    [RelayCommand]
    public void SetCurrentWeek()
    {

        DateTime today = DateTime.Today;

        int diff =
            ((int)today.DayOfWeek + 6) % 7;

        DateTime startOfWeek =
            today.AddDays(-diff);
        Update(() =>
        {
            FromDate = startOfWeek;
            ToDate = startOfWeek.AddDays(6);
        });
    }

    [RelayCommand]
    public void SetCurrentMonth()
    {
        DateTime today = DateTime.Today;

        DateTime startOfMonth =
            new(today.Year, today.Month, 1);
        Update(() =>
        {
            FromDate = startOfMonth;
            ToDate = startOfMonth
                .AddMonths(1)
                .AddDays(-1);
        });
    }

    partial void OnSearchTermChanged(string value)
    {
        OnFilterChanged();
    }

    partial void OnFromDateChanged(DateTime? value)
    {
        OnFilterChanged();
    }

    partial void OnToDateChanged(DateTime? value)
    {
        OnFilterChanged();
    }

    partial void OnSelectedPaymentMethodChanged(PaymentMethod value)
    {
        OnFilterChanged();
    }

    partial void OnSelectedOrderStatusChanged(
        EnumItem<OrderStatus> value)
    {
        OnFilterChanged();
    }
}