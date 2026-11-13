using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Warehouses;
using OrderHub.Application.Features.Warehouses.Contracts;
using OrderHub.Domain.Enums;
using OrderHub.Web.Components.Common;
using OrderHub.Web.Components.Pages.Features.Warehouses.Editor;
using OrderHub.Web.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.Web.Components.Pages.Features.Warehouses;

public partial class Index
{
    private IReadOnlyList<WarehouseListItem> _warehouses = [];
    private string? _searchTerm;
    private string? _error;
    private bool _loading = true;
    private bool _canWrite;
    private bool _canViewStock;
    private bool _dialogOpen;

    private static readonly PageScaffold.CrumbItem[] _scaffoldCrumbs =
    [
        new("المخازن")
    ];

    private int _currentPage;
    private int _rowsPerPage = 10;

    /// <summary>مجدد PageScaffold (1-based) مقابل CurrentPage (0-based) في MudTable.</summary>
    private int FooterCurrentPage => _currentPage + 1;

    private bool CanCreate =>
        _canWrite && !_loading && _error is null;

    private bool IsBusy =>
        _loading || _dialogOpen;

    private IEnumerable<WarehouseListItem> FilteredWarehouses
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_searchTerm))
            {
                return _warehouses;
            }

            string searchTerm = _searchTerm.Trim();

            return _warehouses.Where(warehouse =>
                warehouse.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                warehouse.KeeperDisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (warehouse.Address?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (warehouse.PhoneNumber?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false));
        }
    }

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        _canWrite = false;
        _canViewStock = false;
        _error = null;
        _currentPage = 0;

        try
        {
            var warehouses = await RequestExecutor.ExecuteAsync(new GetWarehousesQuery());
            bool canWrite = await PermissionAccess.IsAllowedAsync(
                PermissionFeature.Warehouses,
                PermissionLevel.Write);
            bool canViewStock = await PermissionAccess.IsAllowedAsync(
                PermissionFeature.Inventory,
                PermissionLevel.Read);

            _warehouses = warehouses.ToList();
            _canWrite = canWrite;
            _canViewStock = canViewStock;
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to load warehouses");
            _error = "تعذر تحميل المخازن. يرجى المحاولة مرة أخرى.";
        }
        finally
        {
            _loading = false;
        }
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

    private Task OpenCreateDialogAsync() => OpenEditorAsync(null);

    private Task OpenEditDialogAsync(WarehouseListItem warehouse) => OpenEditorAsync(warehouse.Id);

    private async Task OpenEditorAsync(int? warehouseId)
    {
        if (IsBusy || !_canWrite)
        {
            return;
        }

        _dialogOpen = true;

        try
        {
            DialogParameters<WarehouseEditorDialog> parameters = new()
            {
                { dialog => dialog.WarehouseId, warehouseId }
            };

            DialogOptions options = new()
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                CloseButton = false,
                CloseOnEscapeKey = false,
                BackdropClick = false
            };

            string title = warehouseId.HasValue ? "تعديل المخزن" : "إضافة مخزن";

            var dialog = await DialogService.ShowAsync<WarehouseEditorDialog>(
                title,
                parameters,
                options);

            DialogResult? result = await dialog.Result;

            if (result is { Canceled: false, Data: true })
            {
                await LoadAsync();
            }
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(
                exception,
                "Failed to open warehouse editor for {WarehouseId}",
                warehouseId);

            Snackbar.Add(
                "تعذر فتح بيانات المخزن. يرجى المحاولة مرة أخرى.",
                Severity.Error);
        }
        finally
        {
            _dialogOpen = false;
        }
    }
}
