using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Returns.Contracts;
using OrderHub.Application.Features.Inventory.Returns.GetOrderReturnContext;
using OrderHub.Application.Features.Inventory.Returns.GetReturnsOverview;
using OrderHub.Application.Interfaces;
using OrderHub.Web.Components.Common;
using OrderHub.Web.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.Web.Components.Pages.Features.Inventory.Returns;

public partial class Index
{
    private static readonly PageScaffold.CrumbItem[] _crumbs =
    [
        new("المخزون", "/warehouses"),
        new("المرتجعات")
    ];

    private ReturnsOverview? _state;
    private string? _error;
    private bool _loading = true;
    private bool _dialogOpen;

    private bool _busy => _loading || _dialogOpen;

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            var result = await Executor.ExecuteAsync(new GetReturnsOverviewQuery());
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
            Logger.LogError(exception, "Failed to load inventory returns overview");
            _error = "تعذر تحميل المرتجعات. يرجى المحاولة مرة أخرى.";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task OpenReturnAsync(ReturnableOrderRow order)
    {
        if (_busy)
            return;

        _dialogOpen = true;
        try
        {
            var contextResult = await Executor.ExecuteAsync(new GetOrderReturnContextQuery(order.OrderId));
            if (!contextResult.IsSuccess)
            {
                Snackbar.Add(contextResult.ErrorMessage, Severity.Error);
                return;
            }

            OrderReturnContext context = contextResult.Value;
            if (!context.Rows.Any(row => row.CanReturn))
            {
                Snackbar.Add("لا توجد كميات قابلة للإرجاع في هذا الطلب.", Severity.Info);
                await LoadAsync();
                return;
            }

            DialogParameters<RecordReturnDialog> parameters = new()
            {
                { dialog => dialog.Context, context }
            };

            var dialog = await Dialogs.ShowAsync<RecordReturnDialog>(
                "تسجيل مرتجع",
                parameters,
                DialogOptions());
            DialogResult? result = await dialog.Result;

            if (result is { Canceled: false })
            {
                Snackbar.Add("تم تسجيل المرتجع بنجاح.", Severity.Success);
                await LoadAsync();
            }
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to open the return dialog for order {OrderId}", order.OrderId);
            Snackbar.Add("تعذر فتح نافذة تسجيل المرتجع. حاول مرة أخرى.", Severity.Error);
        }
        finally
        {
            _dialogOpen = false;
        }
    }

    private async Task OpenSettleAsync(PendingReturnRow pending)
    {
        if (_busy)
            return;

        _dialogOpen = true;
        try
        {
            DialogParameters<SettlePendingReturnDialog> parameters = new()
            {
                { dialog => dialog.Pending, pending }
            };

            var dialog = await Dialogs.ShowAsync<SettlePendingReturnDialog>(
                "استلام مرتجع مستحق",
                parameters,
                DialogOptions());
            DialogResult? result = await dialog.Result;

            if (result is { Canceled: false })
            {
                Snackbar.Add("تم استلام المرتجع المستحق.", Severity.Success);
                await LoadAsync();
            }
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to open the pending-return dialog for allocation {AllocationId}", pending.AllocationId);
            Snackbar.Add("تعذر فتح نافذة الاستلام. حاول مرة أخرى.", Severity.Error);
        }
        finally
        {
            _dialogOpen = false;
        }
    }

    private async Task OpenDisposalAsync(DamagedProductRow product)
    {
        if (_busy)
            return;

        _dialogOpen = true;
        try
        {
            DialogParameters<RequestDisposalDialog> parameters = new()
            {
                { dialog => dialog.WarehouseId, product.WarehouseId },
                { dialog => dialog.ProductId, product.ProductId },
                { dialog => dialog.ProductName, product.ProductName },
                { dialog => dialog.DamagedQty, product.DamagedQty }
            };

            var dialog = await Dialogs.ShowAsync<RequestDisposalDialog>(
                "طلب إتلاف تالف",
                parameters,
                DialogOptions());
            DialogResult? result = await dialog.Result;

            if (result is { Canceled: false })
            {
                Snackbar.Add("تم إرسال طلب الإتلاف للاعتماد.", Severity.Success);
                await LoadAsync();
            }
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to open the disposal dialog for product {ProductId}", product.ProductId);
            Snackbar.Add("تعذر فتح نافذة طلب الإتلاف. حاول مرة أخرى.", Severity.Error);
        }
        finally
        {
            _dialogOpen = false;
        }
    }

    private static DialogOptions DialogOptions() => new()
    {
        MaxWidth = MaxWidth.Medium,
        FullWidth = true,
        CloseButton = false,
        CloseOnEscapeKey = false,
        BackdropClick = false
    };
}
