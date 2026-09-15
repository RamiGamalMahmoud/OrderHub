using CommunityToolkit.Mvvm.ComponentModel;
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

    public void SetToday()
    {
        FromDate = DateTime.Today;
        ToDate = DateTime.Today;
    }

    public void MoveDay(int days)
    {
        if (FromDate is null || ToDate is null)
            return;

        FromDate = FromDate.Value.AddDays(days);
        ToDate = ToDate.Value.AddDays(days);
    }

    public void SetCurrentWeek()
    {
        DateTime today = DateTime.Today;

        int diff =
            ((int)today.DayOfWeek + 6) % 7;

        DateTime startOfWeek =
            today.AddDays(-diff);

        FromDate = startOfWeek;
        ToDate = startOfWeek.AddDays(6);
    }

    public void SetCurrentMonth()
    {
        DateTime today = DateTime.Today;

        DateTime startOfMonth =
            new(today.Year, today.Month, 1);

        FromDate = startOfMonth;
        ToDate = startOfMonth
            .AddMonths(1)
            .AddDays(-1);
    }
}