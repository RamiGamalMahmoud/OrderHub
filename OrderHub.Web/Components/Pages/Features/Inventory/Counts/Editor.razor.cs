using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Counts.CancelCount;
using OrderHub.Application.Features.Inventory.Counts.CompleteCount;
using OrderHub.Application.Features.Inventory.Counts.Contracts;
using OrderHub.Application.Features.Inventory.Counts.GetCountDetails;
using OrderHub.Application.Features.Inventory.Counts.SaveCountLine;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Enums;
using OrderHub.Web.Components.Common;
using OrderHub.Web.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.Web.Components.Pages.Features.Inventory.Counts;

public partial class Editor
{
    [Parameter] public int Id { get; set; }

    private sealed class LineEdit(CountLineRow line)
    {
        public int LineId { get; } = line.LineId;
        public string ProductName { get; } = line.ProductName;
        public string ProductCode { get; } = line.ProductCode;
        public int BookQty { get; } = line.BookQty;
        public int BookDamagedQty { get; } = line.BookDamagedQty;
        public string SoundText { get; set; } = Format(line.CountedQty);
        public string DamagedText { get; set; } = Format(line.CountedDamagedQty);

        public int? CountedQty => line.CountedQty;
        public int? CountedDamagedQty => line.CountedDamagedQty;

        public int? Sound => Parse(SoundText);
        public int? Damaged => Parse(DamagedText);
        public bool SoundInvalid => IsInvalid(SoundText, Sound);
        public bool DamagedInvalid => IsInvalid(DamagedText, Damaged);

        public int? Delta => Sound is int sound ? sound - BookQty : null;
        public int? DamagedDelta => Damaged is int damaged ? damaged - BookDamagedQty : null;

        public bool Dirty =>
            !string.Equals(SoundText.Trim(), Format(line.CountedQty), StringComparison.Ordinal)
            || !string.Equals(DamagedText.Trim(), Format(line.CountedDamagedQty), StringComparison.Ordinal);

        private static string Format(int? value) =>
            value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

        private static int? Parse(string text)
        {
            string trimmed = text.Trim();
            if (trimmed.Length == 0)
                return null;

            return int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                ? value
                : null;
        }

        private static bool IsInvalid(string text, int? parsed) =>
            text.Trim().Length > 0 && (parsed is null or < 0);
    }

    private CountDetails? _count;
    private List<LineEdit> _rows = [];
    private PageScaffold.CrumbItem[] _crumbs =
    [
        new("المخزون", "/warehouses"),
        new("الجرد", "/inventory/counts")
    ];
    private string? _error;
    private bool _loading = true;
    private bool _busy;
    private bool _canApprove;

    private bool _isEditable => _count is { Status: InventoryCountStatus.Open, IsKeeper: true };
    private bool _hasChanges => _rows.Any(row => row.Dirty);
    private bool _invalidInput => _rows.Any(row => row.SoundInvalid || row.DamagedInvalid);

    private bool CanComplete =>
        _count is not null
        && _rows.Count > 0
        && !_invalidInput
        && _rows.All(row => row.Sound is not null && row.Damaged is not null);

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
            _rows = _count.Lines.Select(line => new LineEdit(line)).ToList();
            _canApprove = _count.Status == InventoryCountStatus.UnderReview
                && await PermissionAccess.IsAllowedAsync(PermissionFeature.Inventory, PermissionLevel.Approve);
            _crumbs =
            [
                new("المخزون", "/warehouses"),
                new("الجرد", "/inventory/counts"),
                new($"جرد {_count.WarehouseName}")
            ];
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to load inventory count {CountId}", Id);
            _error = "تعذر تحميل الجرد. يرجى المحاولة مرة أخرى.";
        }
        finally
        {
            _loading = false;
        }
    }

    private void OnSoundInput(LineEdit row, ChangeEventArgs args)
    {
        row.SoundText = args.Value?.ToString() ?? string.Empty;
        StateHasChanged();
    }

    private void OnDamagedInput(LineEdit row, ChangeEventArgs args)
    {
        row.DamagedText = args.Value?.ToString() ?? string.Empty;
        StateHasChanged();
    }

    private static string InputClass(bool invalid) =>
        "w-24 rounded-lg border bg-white px-2 py-1.5 text-xs font-semibold text-slate-800 outline-none transition "
        + (invalid
            ? "border-rose-300 ring-2 ring-rose-100"
            : "border-slate-300 focus:border-brand-500 focus:ring-2 focus:ring-brand-100");

    private async Task SaveAsync()
    {
        if (_busy || _count is null || !_isEditable)
            return;

        _busy = true;
        try
        {
            string? error = await SavePendingAsync();
            if (error is not null)
            {
                Snackbar.Add(error, Severity.Error);
                return;
            }

            Snackbar.Add("تم حفظ العدّ.", Severity.Success);
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to save count lines for {CountId}", Id);
            Snackbar.Add("تعذر حفظ العدّ. حاول مرة أخرى.", Severity.Error);
        }
        finally
        {
            _busy = false;
        }

        await LoadAsync();
    }

    private async Task CompleteAsync()
    {
        if (_busy || _count is null || !_isEditable || !CanComplete)
            return;

        _busy = true;
        try
        {
            string? error = await SavePendingAsync();
            if (error is not null)
            {
                Snackbar.Add(error, Severity.Error);
                return;
            }

            var completed = await Executor.ExecuteAsync(new CompleteCountCommand(_count.Id));
            if (!completed.IsSuccess)
            {
                Snackbar.Add(completed.ErrorMessage, Severity.Error);
                return;
            }

            Snackbar.Add("تم إنهاء العدّ وإرساله لاعتماد المدير.", Severity.Success);
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to complete inventory count {CountId}", Id);
            Snackbar.Add("تعذر إنهاء العدّ. حاول مرة أخرى.", Severity.Error);
        }
        finally
        {
            _busy = false;
        }

        await LoadAsync();
    }

    private async Task<string?> SavePendingAsync()
    {
        foreach (LineEdit row in _rows.Where(row => row.Dirty).ToList())
        {
            var saved = await Executor.ExecuteAsync(new SaveCountLineCommand(
                _count!.Id,
                row.LineId,
                row.Sound,
                row.Damaged));

            if (!saved.IsSuccess)
                return saved.ErrorMessage;
        }

        return null;
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
