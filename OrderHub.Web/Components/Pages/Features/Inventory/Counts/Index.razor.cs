using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Counts.Contracts;
using OrderHub.Application.Features.Inventory.Counts.GetCounts;
using OrderHub.Application.Features.Inventory.Counts.StartCount;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Enums;
using OrderHub.Web.Components.Common;
using OrderHub.Web.Services;
using System;
using System.Threading.Tasks;

namespace OrderHub.Web.Components.Pages.Features.Inventory.Counts;

public partial class Index
{
    private static readonly PageScaffold.CrumbItem[] _crumbs =
    [
        new("المخزون", "/warehouses"),
        new("الجرد")
    ];

    private CountOverview? _state;
    private string? _error;
    private bool _loading = true;
    private bool _dialogOpen;
    private bool _canApprove;

    private bool _busy => _loading || _dialogOpen;

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            var result = await Executor.ExecuteAsync(new GetCountsQuery());
            if (!result.IsSuccess)
            {
                _state = null;
                _error = result.ErrorMessage;
                return;
            }

            _state = result.Value;
            _canApprove = _state.Counts.Any(count => count.Status == InventoryCountStatus.UnderReview)
                && await PermissionAccess.IsAllowedAsync(PermissionFeature.Inventory, PermissionLevel.Approve);
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to load inventory counts overview");
            _error = "تعذر تحميل عمليات الجرد. يرجى المحاولة مرة أخرى.";
        }
        finally
        {
            _loading = false;
        }
    }

    private string ActionLabel(CountRow row) => row.Status switch
    {
        InventoryCountStatus.Open => "متابعة العدّ",
        InventoryCountStatus.UnderReview => _canApprove ? "مراجعة واعتماد" : "عرض",
        _ => "عرض"
    };

    private void OpenCountAsync(CountRow row)
    {
        string target = row.Status == InventoryCountStatus.UnderReview && _canApprove
            ? $"/inventory/counts/{row.Id}/review"
            : $"/inventory/counts/{row.Id}";

        Navigation.NavigateTo(target);
    }

    private async Task OpenStartAsync()
    {
        if (_busy || _state is null || !_state.IsKeeper)
            return;

        _dialogOpen = true;
        try
        {
            DialogParameters<StartCountDialog> parameters = new()
            {
                { dialog => dialog.Products, _state.Products }
            };

            var dialog = await Dialogs.ShowAsync<StartCountDialog>(
                "بدء جرد جديد",
                parameters,
                DialogOptions());
            DialogResult? result = await dialog.Result;

            if (result is { Canceled: false, Data: CountStartResult started })
            {
                Snackbar.Add($"تم بدء الجرد على {started.LineCount} صنفًا.", Severity.Success);
                Navigation.NavigateTo($"/inventory/counts/{started.CountId}");
            }
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to open the start-count dialog");
            Snackbar.Add("تعذر فتح نافذة بدء الجرد. حاول مرة أخرى.", Severity.Error);
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
