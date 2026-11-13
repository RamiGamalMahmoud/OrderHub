using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.IssueOrders.Contracts;
using OrderHub.Application.Features.Inventory.IssueOrders.GetIssueOrderDetails;
using OrderHub.Application.Interfaces;
using OrderHub.Web.Components.Common;
using OrderHub.Web.Services;
using System;
using System.Threading.Tasks;

namespace OrderHub.Web.Components.Pages.Features.Inventory.IssueOrders;

public partial class Details
{
    [Parameter] public int Id { get; set; }

    private StockIssueOrderDetails? _order;
    private PageScaffold.CrumbItem[] _crumbs =
    [
        new("المخزون", "/warehouses"),
        new("أوامر الصرف", "/inventory/issue-orders")
    ];
    private string? _error;
    private bool _loading = true;
    private bool _busy;

    protected override Task OnParametersSetAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            var result = await Executor.ExecuteAsync(new GetIssueOrderDetailsQuery(Id));
            if (!result.IsSuccess)
            {
                _order = null;
                _error = result.ErrorMessage;
                return;
            }

            _order = result.Value;
            _crumbs =
            [
                new("المخزون", "/warehouses"),
                new("أوامر الصرف", "/inventory/issue-orders"),
                new(_order is null
                    ? "أمر صرف"
                    : IssueOrderPresentation.NumberLabel(_order.Number, _order.SequenceYear, _order.SequenceMonth))
            ];
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to load issue order {IssueOrderId}", Id);
            _error = "تعذر تحميل أمر الصرف. يرجى المحاولة مرة أخرى.";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task OpenBatchDialogAsync()
    {
        if (_busy || _order is null || !_order.CanRecordBatches)
            return;

        _busy = true;
        try
        {
            DialogParameters<RecordIssueBatchDialog> parameters = new()
            {
                { dialog => dialog.IssueOrderId, _order.Id },
                { dialog => dialog.OrderLabel, _order.OrderNumber },
                { dialog => dialog.WarehouseName, _order.WarehouseName },
                { dialog => dialog.Lines, _order.Lines }
            };
            DialogOptions options = new()
            {
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                CloseButton = false,
                CloseOnEscapeKey = false,
                BackdropClick = false
            };

            var dialog = await Dialogs.ShowAsync<RecordIssueBatchDialog>("تسجيل دفعة صرف", parameters, options);
            DialogResult? result = await dialog.Result;

            if (result is { Canceled: false })
            {
                Snackbar.Add("تم تسجيل الدفعة بنجاح.", Severity.Success);
                await LoadAsync();
            }
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to open the issue-batch dialog for {IssueOrderId}", Id);
            Snackbar.Add("تعذر فتح نافذة الدفعة. يرجى المحاولة مرة أخرى.", Severity.Error);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task OpenCancelDialogAsync()
    {
        if (_busy || _order is null || !_order.CanRequestCancel)
            return;

        _busy = true;
        try
        {
            DialogParameters<RequestIssueCancelDialog> parameters = new()
            {
                { dialog => dialog.IssueOrderId, _order.Id },
                { dialog => dialog.OrderLabel, _order.OrderNumber },
                { dialog => dialog.WarehouseName, _order.WarehouseName }
            };
            DialogOptions options = new()
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                CloseButton = false,
                CloseOnEscapeKey = false,
                BackdropClick = false
            };

            var dialog = await Dialogs.ShowAsync<RequestIssueCancelDialog>("طلب إلغاء أمر الصرف", parameters, options);
            DialogResult? result = await dialog.Result;

            if (result is { Canceled: false })
            {
                Snackbar.Add("تم إرسال طلب الإلغاء إلى المدير.", Severity.Success);
                await LoadAsync();
            }
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to open the cancellation dialog for {IssueOrderId}", Id);
            Snackbar.Add("تعذر فتح نافذة طلب الإلغاء. يرجى المحاولة مرة أخرى.", Severity.Error);
        }
        finally
        {
            _busy = false;
        }
    }
}
