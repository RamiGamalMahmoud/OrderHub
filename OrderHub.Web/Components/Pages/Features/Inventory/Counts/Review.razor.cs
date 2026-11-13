using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Counts.CancelCount;
using OrderHub.Application.Features.Inventory.Counts.Contracts;
using OrderHub.Application.Features.Inventory.Counts.GetCountDetails;
using OrderHub.Application.Features.Inventory.Counts.ReviewLateMovement;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Enums;
using OrderHub.Web.Components.Common;
using OrderHub.Web.Services;
using System;
using System.Threading.Tasks;

namespace OrderHub.Web.Components.Pages.Features.Inventory.Counts;

public partial class Review
{
    [Parameter] public int Id { get; set; }

    private CountDetails? _count;
    private PageScaffold.CrumbItem[] _crumbs =
    [
        new("المخزون", "/warehouses"),
        new("الجرد", "/inventory/counts")
    ];
    private string? _error;
    private bool _loading = true;
    private bool _busy;
    private bool _canApprove;

    protected override Task OnParametersSetAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            var result = await Executor.ExecuteAsync(new GetCountDetailsQuery(Id));
            if (!result.IsSuccess)
            {
                _count = null;
                _error = result.ErrorMessage;
                return;
            }

            _count = result.Value;
            _canApprove = await PermissionAccess.IsAllowedAsync(
                PermissionFeature.Inventory,
                PermissionLevel.Approve);
            _crumbs =
            [
                new("المخزون", "/warehouses"),
                new("الجرد", "/inventory/counts"),
                new($"مراجعة جرد {_count.WarehouseName}")
            ];
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to load inventory count review {CountId}", Id);
            _error = "تعذر تحميل مراجعة الجرد. يرجى المحاولة مرة أخرى.";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task OpenReviewAsync(CountLineRow line, LateMovementDecision decision)
    {
        if (_busy || _count is null)
            return;

        _busy = true;
        try
        {
            DialogParameters<ReviewLateMovementDialog> parameters = new()
            {
                { dialog => dialog.CountId, _count.Id },
                { dialog => dialog.LineId, line.LineId },
                { dialog => dialog.ProductName, line.ProductName },
                { dialog => dialog.Decision, decision }
            };

            var dialog = await Dialogs.ShowAsync<ReviewLateMovementDialog>(
                decision == LateMovementDecision.Recounted ? "إعادة عدّ الصنف" : "شمل الحركة في العدّ",
                parameters,
                DialogOptions());
            DialogResult? result = await dialog.Result;

            if (result is { Canceled: false })
            {
                Snackbar.Add("تم تسجيل قرار المراجعة.", Severity.Success);
            }
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to open the late-movement review dialog for {CountId}", Id);
            Snackbar.Add("تعذر فتح نافذة المراجعة. حاول مرة أخرى.", Severity.Error);
        }
        finally
        {
            _busy = false;
        }

        await LoadAsync();
    }

    private async Task OpenApproveAsync()
    {
        if (_busy || _count is null || !_canApprove)
            return;

        _busy = true;
        try
        {
            DialogParameters<ApproveCountDialog> parameters = new()
            {
                { dialog => dialog.CountId, _count.Id },
                { dialog => dialog.ExpectedVersion, _count.Version },
                { dialog => dialog.WarehouseName, _count.WarehouseName }
            };

            var dialog = await Dialogs.ShowAsync<ApproveCountDialog>(
                "اعتماد الجرد",
                parameters,
                DialogOptions());
            DialogResult? result = await dialog.Result;

            if (result is { Canceled: false })
            {
                Snackbar.Add("تم اعتماد الجرد وترحيل التسويات.", Severity.Success);
            }
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to open the approve-count dialog for {CountId}", Id);
            Snackbar.Add("تعذر فتح نافذة الاعتماد. حاول مرة أخرى.", Severity.Error);
        }
        finally
        {
            _busy = false;
        }

        await LoadAsync();
    }

    private async Task OpenCancelAsync()
    {
        if (_busy || _count is null)
            return;

        _busy = true;
        try
        {
            DialogParameters<CancelCountDialog> parameters = new()
            {
                { dialog => dialog.CountId, _count.Id },
                { dialog => dialog.WarehouseName, _count.WarehouseName }
            };

            var dialog = await Dialogs.ShowAsync<CancelCountDialog>(
                "إلغاء الجرد",
                parameters,
                DialogOptions());
            DialogResult? result = await dialog.Result;

            if (result is { Canceled: false })
            {
                Snackbar.Add("تم إلغاء الجرد دون تغيير الأرصدة.", Severity.Success);
            }
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to open the cancel-count dialog for {CountId}", Id);
            Snackbar.Add("تعذر فتح نافذة الإلغاء. حاول مرة أخرى.", Severity.Error);
        }
        finally
        {
            _busy = false;
        }

        await LoadAsync();
    }

    private static DialogOptions DialogOptions() => new()
    {
        MaxWidth = MaxWidth.Small,
        FullWidth = true,
        CloseButton = false,
        CloseOnEscapeKey = false,
        BackdropClick = false
    };
}
