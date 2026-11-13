using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Rebuild.Contracts;
using OrderHub.Application.Features.Inventory.Rebuild.GetRebuildHistory;
using OrderHub.Application.Features.Warehouses;
using OrderHub.Application.Features.Warehouses.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Web.Components.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace OrderHub.Web.Components.Pages.Features.Inventory.Rebuild;

public partial class Index
{
    private static readonly PageScaffold.CrumbItem[] _crumbs =
    [
        new("المخزون", "/warehouses"),
        new("إعادة بناء الأرصدة")
    ];

    private IReadOnlyList<RebuildRunRow> _runs = [];
    private IReadOnlyList<WarehouseListItem> _warehouses = [];
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
            Result<IReadOnlyList<RebuildRunRow>> history =
                await Executor.ExecuteAsync(new GetRebuildHistoryQuery());
            if (!history.IsSuccess)
            {
                _runs = [];
                _error = history.ErrorMessage;
                return;
            }

            _runs = history.Value;
            _warehouses = await Executor.ExecuteAsync(new GetWarehousesQuery());
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to load the balance rebuild history");
            _error = "تعذر تحميل سجل إعادة البناء. يرجى المحاولة مرة أخرى.";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task OpenRebuildAsync()
    {
        if (_busy)
            return;

        _dialogOpen = true;
        try
        {
            DialogParameters<RebuildBalancesDialog> parameters = new()
            {
                { dialog => dialog.Warehouses, _warehouses }
            };

            var dialog = await Dialogs.ShowAsync<RebuildBalancesDialog>(
                "إعادة بناء الأرصدة",
                parameters,
                DialogOptions());
            DialogResult? result = await dialog.Result;

            if (result is { Canceled: false, Data: RebuildBalancesResult rebuild })
            {
                Snackbar.Add(
                    rebuild.ChangedPairs == 0
                        ? $"اكتملت إعادة البناء: فُحص {Format(rebuild.InspectedPairs)} زوجًا ولم تتغير أي أرصدة."
                        : $"اكتملت إعادة البناء: فُحص {Format(rebuild.InspectedPairs)} زوجًا وتغيّر {Format(rebuild.ChangedPairs)}.",
                    Severity.Success);

                await LoadAsync();
                await OpenLogAsync(rebuild.LogId);
            }
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to open the balance rebuild dialog");
            Snackbar.Add("تعذر فتح نافذة إعادة بناء الأرصدة. حاول مرة أخرى.", Severity.Error);
        }
        finally
        {
            _dialogOpen = false;
        }
    }

    private async Task OpenLogAsync(int logId)
    {
        try
        {
            DialogParameters<RebuildLogDialog> parameters = new()
            {
                { dialog => dialog.LogId, logId }
            };

            await Dialogs.ShowAsync<RebuildLogDialog>(
                "تفاصيل إعادة البناء",
                parameters,
                DialogOptions());
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to open the rebuild log dialog");
            Snackbar.Add("تعذر فتح تفاصيل إعادة البناء. حاول مرة أخرى.", Severity.Error);
        }
    }

    private static string ScopeLabel(RebuildRunRow run) =>
        run.Scope == InventoryRebuildScope.AllWarehouses
            ? "كل المخازن"
            : run.WarehouseName ?? "مخزن محدد";

    private static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static DialogOptions DialogOptions() => new()
    {
        MaxWidth = MaxWidth.Medium,
        FullWidth = true,
        CloseButton = false,
        CloseOnEscapeKey = false,
        BackdropClick = false
    };
}
