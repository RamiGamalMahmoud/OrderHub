using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory;
using OrderHub.Application.Features.Inventory.Contracts;
using OrderHub.Application.Features.Inventory.DeleteStockAttachment;
using OrderHub.Application.Features.Inventory.GetStockInContext;
using OrderHub.Application.Features.Inventory.RecordStockIn;
using OrderHub.Application.Features.Inventory.UploadStockAttachment;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Features.Products.Search;
using OrderHub.Application.Features.Setup.Suppliers;
using OrderHub.Application.Features.Setup.Suppliers.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Enums;
using OrderHub.Web.Components.Common;
using OrderHub.Web.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Web.Components.Pages.Features.Inventory.StockIn;

public partial class StockIn
{
    private static readonly PageScaffold.CrumbItem[] _scaffoldCrumbs =
    [
        new("تسجيل توريد")
    ];

    private StockInState? _state;
    private IReadOnlyList<SupplierInfo> _suppliers = [];
    private StockReceiptDetails? _lastSaved;
    private string? _accessMessage;
    private string? _loadError;
    private string? _saveError;
    private string? _attachmentError;
    private string _attachmentDescription = string.Empty;
    private string _attachmentInputKey = Guid.NewGuid().ToString("N");
    private bool _loading = true;
    private bool _saving;
    private bool _uploading;

    private bool IsBusy => _saving || _uploading;

    private bool CanSubmit =>
        _state is not null &&
        _state.CanReadProducts &&
        _state.HasCompleteLine &&
        !_state.HasInvalidLine &&
        !IsBusy;

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        _accessMessage = null;
        _loadError = null;

        try
        {
            var result = await RequestExecutor.ExecuteAsync(new GetStockInContextQuery());

            if (!result.IsSuccess)
            {
                _state = null;
                _accessMessage = result.ErrorMessage;
                return;
            }

            ApplyContext(result.Value);

            if (result.Value.CanSelectSupplier)
            {
                IEnumerable<SupplierInfo> suppliers = await RequestExecutor.ExecuteAsync(new GetSuppliersInfoQuery());
                _suppliers = suppliers.ToList();
            }
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to load the stock-in context");
            _loadError = "تعذر تحميل بيانات التوريد. يرجى المحاولة مرة أخرى.";
        }
        finally
        {
            _loading = false;
        }
    }

    private void ApplyContext(StockInContext context)
    {
        _state = new StockInState(context);

        if (!context.IsOpeningAllowed)
            _state.ReceiptType = StockReceiptType.StockIn;
    }

    private async Task<IEnumerable<ProductLookupItem>> SearchProductsAsync(
        string value,
        CancellationToken cancellationToken)
    {
        if (_state is null || !_state.CanReadProducts)
            return [];

        try
        {
            var page = await RequestExecutor.ExecuteAsync(
                new SearchProductsQuery(value ?? string.Empty, 1, 20),
                cancellationToken);

            return page.Items;
        }
        catch (AuthorizationDeniedException)
        {
            return [];
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to search products for stock-in");
            return [];
        }
    }

    private void OnProductSelected(StockInLineState line, ProductLookupItem product)
    {
        line.Product = product;

        if (product is null || _state is null)
            return;

        StockInLineState? duplicate = _state.Lines
            .FirstOrDefault(other => other != line && other.Product?.Id == product.Id);

        if (duplicate is null)
            return;

        duplicate.Quantity = checked(duplicate.Quantity + Math.Max(line.Quantity, 1));
        line.Product = null;
        line.Quantity = 1;

        if (_state.Lines.Count > 1)
            _state.Lines.Remove(line);
    }

    private void OnSupplierChanged(SupplierInfo? supplier) => _state!.Supplier = supplier;

    private void AddLine() => _state?.AddLine();

    private void RemoveLine(StockInLineState line) => _state?.RemoveLine(line);

    private async Task OnFileSelectedAsync(InputFileChangeEventArgs args)
    {
        if (_state is null || args.File is null || _uploading)
            return;

        _attachmentError = null;

        if (_state.Attachments.Count >= 10)
        {
            _attachmentError = "يمكن إرفاق 10 ملفات كحد أقصى لكل توريد.";
            return;
        }

        if (args.File.Size > StockAttachmentRules.MaxSizeBytes)
        {
            _attachmentError = "حجم الملف يتجاوز 5 ميجابايت.";
            return;
        }

        _uploading = true;

        try
        {
            await using Stream stream = args.File.OpenReadStream(StockAttachmentRules.MaxSizeBytes);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);

            var result = await RequestExecutor.ExecuteAsync(new UploadStockAttachmentCommand(
                _state.WarehouseId,
                args.File.Name,
                _attachmentDescription,
                buffer.ToArray()));

            if (!result.IsSuccess)
            {
                _attachmentError = result.ErrorMessage;
                return;
            }

            _state.Attachments.Add(result.Value);
            _attachmentDescription = string.Empty;
            _attachmentInputKey = Guid.NewGuid().ToString("N");
            Snackbar.Add("تم رفع المرفق.", Severity.Success);
        }
        catch (IOException)
        {
            _attachmentError = "حجم الملف يتجاوز 5 ميجابايت.";
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to upload a stock attachment");
            _attachmentError = "تعذر رفع المرفق. يرجى المحاولة مرة أخرى.";
        }
        finally
        {
            _uploading = false;
        }
    }

    private async Task RemoveAttachmentAsync(StockAttachmentDraft attachment)
    {
        if (_state is null || IsBusy)
            return;

        _attachmentError = null;

        try
        {
            var result = await RequestExecutor.ExecuteAsync(
                new DeleteStockAttachmentCommand(attachment.Id));

            if (!result.IsSuccess)
            {
                _attachmentError = result.ErrorMessage;
                return;
            }

            _state.Attachments.Remove(attachment);
            Snackbar.Add("تم حذف المرفق.", Severity.Info);
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to delete stock attachment {AttachmentId}", attachment.Id);
            _attachmentError = "تعذر حذف المرفق. يرجى المحاولة مرة أخرى.";
        }
    }

    private async Task SaveAsync()
    {
        if (_state is null || _saving)
            return;

        _saveError = null;

        if (!_state.HasCompleteLine)
        {
            _saveError = "أضف صنفًا واحدًا على الأقل مع الكمية.";
            return;
        }

        if (_state.HasInvalidLine)
        {
            _saveError = "كمية كل صنف يجب أن تكون أكبر من صفر.";
            return;
        }

        _saving = true;

        try
        {
            var input = new StockInInput(
                _state.WarehouseId,
                _state.ReceiptType,
                _state.Supplier?.Id,
                _state.ReferenceNote,
                _state.Lines
                    .Where(line => line.Product is not null)
                    .Select(line => line.ToInput())
                    .ToList(),
                _state.Attachments.Select(attachment => attachment.Id).ToList());

            var result = await RequestExecutor.ExecuteAsync(new RecordStockInCommand(input));

            if (!result.IsSuccess)
            {
                _saveError = result.ErrorMessage;
                Snackbar.Add(result.ErrorMessage, Severity.Error);
                return;
            }

            _lastSaved = result.Value;
            _state.ResetAfterSave();
            Snackbar.Add("تم حفظ التوريد بنجاح.", Severity.Success);

            await RefreshContextAsync();
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to record stock-in for warehouse {WarehouseId}", _state.WarehouseId);
            _saveError = "تعذر حفظ التوريد. يرجى المحاولة مرة أخرى.";
        }
        finally
        {
            _saving = false;
        }
    }

    private async Task RefreshContextAsync()
    {
        if (_state is null)
            return;

        try
        {
            var result = await RequestExecutor.ExecuteAsync(new GetStockInContextQuery());

            if (!result.IsSuccess)
            {
                _state = null;
                _accessMessage = result.ErrorMessage;
                return;
            }

            ApplyContext(result.Value);
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to refresh the stock-in context");
        }
    }

    private static string ReceiptTypeLabel(StockReceiptType receiptType) =>
        receiptType == StockReceiptType.Opening ? "رصيد افتتاحي" : "توريد";

    private static string AttachmentIcon(string contentType) => contentType switch
    {
        "application/pdf" => Icons.Material.Filled.PictureAsPdf,
        _ => Icons.Material.Filled.Image
    };

    private static string FormatSize(long sizeBytes) =>
        sizeBytes >= 1024 * 1024
            ? (sizeBytes / (1024d * 1024d)).ToString("0.#", CultureInfo.InvariantCulture) + " ميجابايت"
            : Math.Max(1, (long)Math.Ceiling(sizeBytes / 1024d)).ToString(CultureInfo.InvariantCulture) + " كيلوبايت";
}
