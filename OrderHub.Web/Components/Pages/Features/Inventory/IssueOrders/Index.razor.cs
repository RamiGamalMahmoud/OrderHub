using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.IssueOrders.Contracts;
using OrderHub.Application.Features.Inventory.IssueOrders.GetIssueOrders;
using OrderHub.Application.Interfaces;
using OrderHub.Web.Components.Common;
using OrderHub.Web.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.Web.Components.Pages.Features.Inventory.IssueOrders;

public partial class Index
{
    private static readonly PageScaffold.CrumbItem[] _crumbs =
    [
        new("المخزون", "/warehouses"),
        new("أوامر الصرف")
    ];

    private StockIssueOrderList? _state;
    private string? _searchTerm;
    private string? _error;
    private bool _loading = true;
    private bool _dialogOpen;
    private bool _queryHandled;
    private int _currentPage;
    private int _rowsPerPage = 10;

    [SupplyParameterFromQuery(Name = "orderId")]
    public int? OrderId { get; set; }

    private int FooterCurrentPage => _currentPage + 1;

    private bool _busy => _loading || _dialogOpen;

    private IEnumerable<StockIssueOrderSummary> FilteredItems
    {
        get
        {
            if (_state is null)
                return [];

            if (string.IsNullOrWhiteSpace(_searchTerm))
                return _state.Items;

            string term = _searchTerm.Trim();
            return _state.Items.Where(item =>
                item.OrderNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.WarehouseName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.RequestedByName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                IssueOrderPresentation.NumberLabel(item.Number, item.SequenceYear, item.SequenceMonth)
                    .Contains(term, StringComparison.OrdinalIgnoreCase));
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
        await HandleQueryAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        _currentPage = 0;
        try
        {
            var result = await Executor.ExecuteAsync(new GetIssueOrdersQuery());
            if (!result.IsSuccess)
            {
                _state = null;
                _error = result.ErrorMessage;
                return;
            }

            _state = result.Value;
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to load stock issue orders");
            _error = "تعذر تحميل أوامر الصرف. يرجى المحاولة مرة أخرى.";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task HandleQueryAsync()
    {
        if (_queryHandled || OrderId is not int orderId || orderId <= 0 || _state?.CanCreate != true)
            return;

        _queryHandled = true;
        await OpenCreateDialogAsync(orderId);
        Navigation.NavigateTo("/inventory/issue-orders", replace: true);
    }

    private Task OnFooterPageChanged(int page)
    {
        _currentPage = page - 1;
        return Task.CompletedTask;
    }

    private Task OnFooterRowsPerPageChanged(int rowsPerPage)
    {
        _rowsPerPage = rowsPerPage;
        _currentPage = 0;
        return Task.CompletedTask;
    }

    private Task OpenCreateDialogAsync() => OpenCreateDialogAsync(null);

    private async Task OpenCreateDialogAsync(int? initialOrderId)
    {
        if (_busy || _state?.CanCreate != true)
            return;

        _dialogOpen = true;
        try
        {
            DialogParameters<CreateIssueOrderDialog> parameters = new()
            {
                { dialog => dialog.InitialOrderId, initialOrderId }
            };
            DialogOptions options = new()
            {
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                CloseButton = false,
                CloseOnEscapeKey = false,
                BackdropClick = false
            };

            var dialog = await Dialogs.ShowAsync<CreateIssueOrderDialog>("أمر صرف جديد", parameters, options);
            DialogResult? result = await dialog.Result;

            if (result is { Canceled: false, Data: int createdId })
            {
                Snackbar.Add("تم إنشاء أمر الصرف بنجاح.", Severity.Success);
                Navigation.NavigateTo($"/inventory/issue-orders/{createdId}");
                return;
            }

            if (result is { Canceled: false })
                await LoadAsync();
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to open the issue-order creation dialog");
            Snackbar.Add("تعذر فتح نافذة أمر الصرف. يرجى المحاولة مرة أخرى.", Severity.Error);
        }
        finally
        {
            _dialogOpen = false;
        }
    }
}
