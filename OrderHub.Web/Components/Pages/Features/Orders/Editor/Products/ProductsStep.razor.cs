using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderHub.Application.Features.Orders;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.Web.Components.Pages.Features.Orders.Editor.State;
using static OrderHub.Web.Components.Pages.Features.Orders.Editor.Products.AddProductDialog;

namespace OrderHub.Web.Components.Pages.Features.Orders.Editor.Products;

public partial class ProductsStep : IDisposable
{
    private bool _disposed;
    private bool _openingDialog;
    private IDialogReference? _addDialog;
    private OrderItemEditorState? _editingItem;
    private bool _isEditorOpen;

    [Parameter]
    public Guid SessionId { get; set; }
    [Inject]
    private IRequestExecutor RequestExecutor { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private ILogger<ProductsStep> Logger { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public required List<OrderItemEditorState> Items { get; set; }

    [Parameter]
    public EventCallback ItemsChanged { get; set; }

    private async Task AddProductAsync()
    {
        if (_disposed || _openingDialog || _addDialog is not null)
            return;
        _openingDialog = true;
        try
        {
        DialogOptions options = new()
        {
            Position = DialogPosition.TopCenter,
            CloseOnEscapeKey = true,
            CloseButton = true,
            DefaultFocus = DefaultFocus.Element
        };

        DialogParameters<AddProductDialog> parameters = new()
        {
            {
                x => x.ProductAdded,
                (Func<AddProductResult, Task<Result>>)OnProductAddedAsync
            }
        };

        var dialog = await DialogService.ShowAsync<AddProductDialog>(
            "إضافة منتج",
            parameters,
            options);
        _addDialog = dialog;
        if (_disposed)
            dialog.Close(DialogResult.Cancel());
        await dialog.Result;
        }
        finally
        {
            _openingDialog = false;
            _addDialog = null;
        }
    }

    private async Task<Result> OnProductAddedAsync(AddProductResult result)
    {
        if (_disposed)
            return Result.Failure("جلسة التحرير لم تعد متاحة.");
        var sessionId = SessionId;
        var items = Items;
        GetOrderItemEditor.OrderItemEditorDto product;
        try
        {
            product = await RequestExecutor.ExecuteAsync(
                new GetOrderItemEditor.Query(result.Product.Id));
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to load product {ProductId} for addition.", result.Product.Id);
            return Result.Failure("تعذر تحميل بيانات المنتج. لم تتم الإضافة؛ حاول مرة أخرى.");
        }

        if (product is null)
            return Result.Failure("المنتج لم يعد متاحاً. اختر منتجاً آخر.");

        if (_disposed || SessionId != sessionId || !ReferenceEquals(Items, items))
            return Result.Failure("تغيرت جلسة التحرير. لم تتم إضافة المنتج.");

        OrderItemEditorState item = new()
        {
            ProductId = product.ProductId,
            ProductName = product.Name,
            CategoryName = product.CategoryName,

            UnitPrice = result.UnitPrice,
            Quantity = result.Quantity,
            VatRate = result.VatRate,

            Suppliers = product.Suppliers
                .Select(supplier => new OrderItemSupplierEditorState(
                    supplier.Id,
                    supplier.Name))
                .ToList(),

            Properties = product.Properties
                .Select(property => new OrderItemPropertyEditorState
                {
                    PropertyId = property.Id,
                    Name = property.Name,
                    IsRequired = property.IsRequired,
                    Type = property.PropertyType,

                    Options = property.Options
                        .Select(option => new OrderItemPropertyOptionEditorState(
                            option.Id,
                            option.Name))
                        .ToList()
                })
                .ToList()
        };

        Items.Add(item);
        await ItemsChanged.InvokeAsync();
        await InvokeAsync(StateHasChanged);
        return Result.Success();
    }

    private void EditItem(OrderItemEditorState item)
    {
        if (_disposed || !Items.Contains(item))
            return;
        _editingItem = item;
        _isEditorOpen = true;
    }

    private async Task<Result> CommitItemAsync(OrderItemEditorState original, Guid sessionId, OrderItemEditorState working)
    {
        if (_disposed || SessionId != sessionId || !Items.Contains(original))
            return Result.Failure("المنتج أو جلسة التحرير لم يعد متاحاً. لم يتم الحفظ.");
        if (working.UnitPrice <= 0 || working.Quantity <= 0 || working.VatRate is < 0 or > 100)
            return Result.Failure("تحقق من السعر والكمية والضريبة قبل الحفظ.");
        if (!original.Properties.Select(p => p.PropertyId).SequenceEqual(working.Properties.Select(p => p.PropertyId)))
            return Result.Failure("تغيرت خصائص المنتج. افتح المحرر مرة أخرى.");

        original.UnitPrice = working.UnitPrice;
        original.Quantity = working.Quantity;
        original.VatRate = working.VatRate;
        original.SupplierId = working.SupplierId;
        for (var index = 0; index < original.Properties.Count; index++)
            original.Properties[index].Value = working.Properties[index].Value;
        await ItemsChanged.InvokeAsync();
        return Result.Success();
    }

    private async Task RemoveItem(OrderItemEditorState item)
    {
        if (!_disposed && Items.Remove(item))
            await ItemsChanged.InvokeAsync();
    }

    public void Dispose()
    {
        _disposed = true;
        _addDialog?.Close(DialogResult.Cancel());
    }
}
