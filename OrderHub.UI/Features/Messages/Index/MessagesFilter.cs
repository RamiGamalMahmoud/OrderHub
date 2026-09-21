using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrderHub.Application.Common.Lookups;
using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderHub.UI.Features.Messages.Index;

public partial class MessagesFilter : ObservableObject
{
    private bool _suppressChanged;

    public event EventHandler FilterChanged;

    [ObservableProperty]
    private string _searchTerm;

    [ObservableProperty]
    private EnumItem<OutboxMessageStatus> _selectedStatus;

    [ObservableProperty]
    private DateTime? _fromDate = DateTime.Today;

    [ObservableProperty]
    private DateTime? _toDate = DateTime.Today;

    public IEnumerable<EnumItem<OutboxMessageStatus>> StatusFilters =>
        new[]
        {
            new EnumItem<OutboxMessageStatus>(
                default,
                "الكل")
        }
        .Concat(EnumItems.For<OutboxMessageStatus>());

    public OutboxMessageStatus? Status =>
        SelectedStatus is null ||
        SelectedStatus.DisplayName == "الكل"
            ? null
            : SelectedStatus.Value;

    partial void OnSearchTermChanged(string value)
    {
        OnFilterChanged();
    }

    partial void OnSelectedStatusChanged(
        EnumItem<OutboxMessageStatus> value)
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

    public void Initialize()
    {
        _suppressChanged = true;

        try
        {
            SelectedStatus =
                StatusFilters.FirstOrDefault();
        }
        finally
        {
            _suppressChanged = false;
        }
    }

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

    private void OnFilterChanged()
    {
        if (_suppressChanged)
            return;

        FilterChanged?.Invoke(
            this,
            EventArgs.Empty);
    }
}