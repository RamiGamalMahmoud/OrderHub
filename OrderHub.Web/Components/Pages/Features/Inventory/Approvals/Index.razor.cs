using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Approvals.Contracts;
using OrderHub.Application.Features.Inventory.Approvals.GetApprovals;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Enums;
using OrderHub.Web.Components.Common;
using OrderHub.Web.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.Web.Components.Pages.Features.Inventory.Approvals;

public partial class Index
{
    private static readonly PageScaffold.CrumbItem[] _crumbs =
    [
        new("المخزون", "/warehouses"),
        new("الاعتمادات")
    ];

    private IReadOnlyList<InventoryApprovalProjectionRow> _rows = [];
    private string? _searchTerm;
    private string? _error;
    private InventoryApprovalStatus? _statusFilter = InventoryApprovalStatus.Pending;
    private InventoryApprovalRequestType? _typeFilter;
    private int _currentPage;
    private int _rowsPerPage = 10;
    private bool _loading = true;
    private bool _dialogOpen;
    private int? _currentUserId;

    private bool _busy => _loading || _dialogOpen;

    private int FooterCurrentPage => _currentPage + 1;

    private IEnumerable<InventoryApprovalProjectionRow> FilteredRows
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_searchTerm))
                return _rows;

            string term = _searchTerm.Trim();
            return _rows.Where(row =>
                (row.OrderNumber?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (row.IssueOrderOrderNumber?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (row.IssueOrderWarehouseName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (row.MovementWarehouseName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (row.MovementProductName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (row.DisposalProductName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (row.DisposalWarehouseName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                row.RequestedByName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                row.Reason.Contains(term, StringComparison.OrdinalIgnoreCase));
        }
    }

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        _currentPage = 0;
        try
        {
            _currentUserId = (await AuthenticatedUser.GetIdentityAsync()).Id;
            var result = await Executor.ExecuteAsync(
                new GetApprovalsQuery(_statusFilter, _typeFilter));
            if (!result.IsSuccess)
            {
                _rows = [];
                _error = result.ErrorMessage;
                return;
            }

            _rows = result.Value;
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to load inventory approvals");
            _error = "تعذر تحميل طلبات الاعتماد. يرجى المحاولة مرة أخرى.";
        }
        finally
        {
            _loading = false;
        }
    }

    private bool IsSelfRequest(InventoryApprovalProjectionRow row) =>
        row.RequestType != InventoryApprovalRequestType.MovementReversal &&
        _currentUserId is int userId &&
        row.RequestedByUserId == userId;

    private async Task OpenDecisionAsync(InventoryApprovalProjectionRow row)
    {
        if (_busy)
            return;

        _dialogOpen = true;
        try
        {
            DialogParameters<DecideApprovalDialog> parameters = new()
            {
                { dialog => dialog.Row, row }
            };
            var dialog = await Dialogs.ShowAsync<DecideApprovalDialog>(
                "قرار اعتماد",
                parameters,
                DialogOptions());
            DialogResult? result = await dialog.Result;
            await HandleDecisionResultAsync(result);
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to open the approval decision dialog for {ApprovalId}", row.Id);
            Snackbar.Add("تعذر فتح نافذة القرار. يرجى المحاولة مرة أخرى.", Severity.Error);
        }
        finally
        {
            _dialogOpen = false;
        }
    }

    private async Task OpenReviewAsync(InventoryApprovalProjectionRow row)
    {
        if (_busy)
            return;

        _dialogOpen = true;
        try
        {
            DialogParameters<EditPayloadReviewDialog> parameters = new()
            {
                { dialog => dialog.Row, row }
            };
            var dialog = await Dialogs.ShowAsync<EditPayloadReviewDialog>(
                "مراجعة تعديل الطلب",
                parameters,
                DialogOptions());
            DialogResult? result = await dialog.Result;
            await HandleDecisionResultAsync(result);
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to open the edit payload review for {ApprovalId}", row.Id);
            Snackbar.Add("تعذر تحميل فروقات التعديل. يرجى المحاولة مرة أخرى.", Severity.Error);
        }
        finally
        {
            _dialogOpen = false;
        }
    }

    private async Task HandleDecisionResultAsync(DialogResult? result)
    {
        if (result is not { Canceled: false })
            return;

        await LoadAsync();
    }

    private static DialogOptions DialogOptions() => new()
    {
        MaxWidth = MaxWidth.Medium,
        FullWidth = true,
        CloseButton = false,
        CloseOnEscapeKey = false,
        BackdropClick = false
    };

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
}
