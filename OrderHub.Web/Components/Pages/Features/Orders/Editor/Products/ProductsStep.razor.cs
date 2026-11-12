
using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.GetStockAvailability;
using OrderHub.Application.Features.Orders.GetItemEditor;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Features.Orders;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Services.Pricing;
using OrderHub.Web.Components.Pages.Features.Orders.Editor.State;

namespace OrderHub.Web.Components.Pages.Features.Orders.Editor.Products;

public partial class ProductsStep : IDisposable
{
    private bool _disposed;
    private bool _openingDialog;
    private IDialogReference? _addDialog;

    private OrderItemEditorState? _editingItem;
    private bool _isEditorOpen;
    private bool _isAddingItem;

    private readonly Dictionary<int, StockSourceAvailability> _availability = [];

    private sealed record StockSourceAvailability(
        IReadOnlyList<StockAvailabilityEditorState> Rows,
        string? Warning);

    private StockSourceAvailability CurrentAvailability =>
        _editingItem is not null && _availability.TryGetValue(_editingItem.ProductId, out var value)
            ? value
            : new StockSourceAvailability([], null);

    private static PricingItemResult PricingOf(OrderItemEditorState item) =>
        PricingCalculator.CalculateItem(
            new PricingItem(
                item.ProductId,
                item.ProductName,
                item.Quantity,
                item.UnitPrice,
                item.VatRate));

    [Parameter]
    public Guid SessionId { get; set; }

    [Inject]
    private IRequestExecutor RequestExecutor { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private ILogger<ProductsStep> Logger { get; set; } = default!;

    [Parameter, EditorRequired]
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

            var dialog = await DialogService.ShowAsync<AddProductDialog>(
                "إضافة منتج",
                new DialogParameters<AddProductDialog>
                {
                    { x => x.ProductSelected, (Func<ProductLookupItem, Task<Result>>)OpenNewProductEditorAsync }
                },
                options);

            _addDialog = dialog;

            if (_disposed)
            {
                dialog.Close(DialogResult.Cancel());
                return;
            }

            await dialog.Result;
        }
        catch (Exception exception)
        {
            Logger.LogError(
                exception,
                "Failed to open the product selection or editor dialog.");

            if (!_disposed)
                Snackbar.Add("تعذر فتح محرر المنتج.", Severity.Error);
        }
        finally
        {
            _openingDialog = false;
            _addDialog = null;
        }
    }

    private async Task<StockSourceAvailability> GetAvailabilityAsync(int productId)
    {
        if (_availability.TryGetValue(productId, out var cached))
            return cached;

        StockSourceAvailability availability;
        try
        {
            var rows = await RequestExecutor.ExecuteAsync(new GetStockAvailabilityQuery(productId));
            availability = new StockSourceAvailability(
                rows.Select(row => new StockAvailabilityEditorState(
                    row.WarehouseId, row.WarehouseName, row.StockOnHand, row.ReservedOnHand, row.Available)).ToList(),
                null);
        }
        catch (AuthorizationDeniedException)
        {
            availability = new StockSourceAvailability(
                [], "لا تملك صلاحية عرض أرصدة المخزون. يمكنك تخصيص الكمية من المورد فقط.");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to load stock availability for product {ProductId}.", productId);
            availability = new StockSourceAvailability(
                [], "تعذر تحميل أرصدة المخزون. يمكنك تخصيص الكمية من المورد فقط.");
        }

        _availability[productId] = availability;
        return availability;
    }

    private async Task<Result> OpenNewProductEditorAsync(ProductLookupItem selectedProduct)
    {
        if (_disposed || _isEditorOpen || _editingItem is not null)
            return Result.Failure("أغلق محرر المنتج الحالي قبل اختيار منتج آخر.");

        var sessionId = SessionId;
        var items = Items;

        OrderItemEditorData product;

        try
        {
            product = await RequestExecutor.ExecuteAsync(
                new GetOrderItemEditorQuery(selectedProduct.Id));
        }
        catch (Exception exception)
        {
            Logger.LogError(
                exception,
                "Failed to load product {ProductId} for addition.",
                selectedProduct.Id);

            return Result.Failure("تعذر تحميل بيانات المنتج. حاول مرة أخرى.");
        }

        if (_disposed ||
            SessionId != sessionId ||
            !ReferenceEquals(Items, items))
        {
            return Result.Failure("تغيرت جلسة التحرير. لم يتم فتح المحرر.");
        }

        if (product is null)
        {
            return Result.Failure("المنتج لم يعد متاحاً. اختر منتجاً آخر.");
        }

        await GetAvailabilityAsync(product.ProductId);

        var draft = new OrderItemEditorState
        {
            ProductId = product.ProductId,
            ProductName = product.Name,
            CategoryName = product.CategoryName,

            // قيم ابتدائية قابلة للتعديل داخل المحرر.
            UnitPrice = selectedProduct.Price,
            Quantity = 1,
            VatRate = 0,

            Suppliers = product.Suppliers
                .Select(supplier =>
                    new OrderItemSupplierEditorState(
                        supplier.Id,
                        supplier.Name))
                .ToList(),

            Properties = product.Properties
                .Select(property =>
                    new OrderItemPropertyEditorState
                    {
                        PropertyId = property.Id,
                        Name = property.Name,
                        IsRequired = property.IsRequired,
                        Type = property.PropertyType,
                        Options = property.Options
                            .Select(option =>
                                new OrderItemPropertyOptionEditorState(
                                    option.Id,
                                    option.Name))
                            .ToList()
                    })
                .ToList()
        };

        _editingItem = draft;
        _isAddingItem = true;
        _isEditorOpen = true;
        await InvokeAsync(StateHasChanged);
        return Result.Success();
    }

    private async Task EditItem(OrderItemEditorState item)
    {
        if (_disposed || !Items.Contains(item))
            return;

        // Already editing this item — don't reinitialize the drawer.
        if (_isEditorOpen && ReferenceEquals(_editingItem, item) && !_isAddingItem)
            return;

        await GetAvailabilityAsync(item.ProductId);

        _editingItem = item;
        _isAddingItem = false;
        _isEditorOpen = true;
    }

    private void OnGridRowClicked(OrderItemEditorState? item)
    {
        if (item is null)
            return;

        // Don't hijack the flow while the "add product" drawer is open.
        if (_isEditorOpen && _isAddingItem)
            return;

        _ = EditItem(item);
    }

    private Task OnEditorOpenChanged(bool open)
    {
        _isEditorOpen = open;

        if (!open)
        {
            _editingItem = null;
            _isAddingItem = false;
        }

        return Task.CompletedTask;
    }

    private async Task<Result> CommitItemAsync(
        OrderItemEditorState original,
        Guid sessionId,
        OrderItemEditorState working)
    {
        var isAdding = _isAddingItem
            && ReferenceEquals(_editingItem, original)
            && !Items.Contains(original);

        if (_disposed ||
            SessionId != sessionId ||
            (!Items.Contains(original) && !isAdding))
        {
            return Result.Failure(
                "المنتج أو جلسة التحرير لم يعد متاحاً. لم يتم الحفظ.");
        }

        if (working.UnitPrice <= 0 ||
            working.Quantity <= 0 ||
            working.VatRate is < 0 or > 100)
        {
            return Result.Failure(
                "تحقق من السعر والكمية والضريبة قبل الحفظ.");
        }

        var allocationError = ValidateAllocations(working);
        if (allocationError is not null)
            return Result.Failure(allocationError);

        if (!original.Properties
                .Select(p => p.PropertyId)
                .SequenceEqual(
                    working.Properties.Select(p => p.PropertyId)))
        {
            return Result.Failure(
                "تغيرت خصائص المنتج. افتح المحرر مرة أخرى.");
        }

        for (var index = 0; index < original.Properties.Count; index++)
        {
            var error = original.Properties[index]
                .ValidateValue(working.Properties[index].Value);

            if (error is not null)
                return Result.Failure(error);
        }

        var hasChanges =
            original.UnitPrice != working.UnitPrice ||
            original.Quantity != working.Quantity ||
            original.VatRate != working.VatRate ||
            original.SupplierId != working.SupplierId ||
            !original.Properties
                .Select(p => p.Value)
                .SequenceEqual(
                    working.Properties.Select(p => p.Value)) ||
            !AllocationKey(original).SequenceEqual(AllocationKey(working));

        // في التعديل، لا حاجة لتحديث الحالة عند عدم وجود تغييرات.
        // أما الإضافة، فيجب إدراج المسودة حتى لو لم يعدل المستخدم القيم الافتراضية.
        if (!isAdding && !hasChanges)
            return Result.Success();

        original.UnitPrice = working.UnitPrice;
        original.Quantity = working.Quantity;
        original.VatRate = working.VatRate;
        original.SupplierId = working.SupplierId;
        original.Allocations.Clear();
        original.Allocations.AddRange(working.Allocations.Select(allocation => allocation.Clone()));

        for (var index = 0; index < original.Properties.Count; index++)
        {
            original.Properties[index].Value =
                working.Properties[index].Value;
        }

        if (isAdding)
        {
            // التحقق من عدم التكرار قبل إدراج المسودة.
            if (Items.Contains(original))
                return Result.Failure("تمت إضافة هذا المنتج بالفعل.");

            Items.Add(original);
            _isAddingItem = false;
        }

        await ItemsChanged.InvokeAsync();

        return Result.Success();
    }

    private static IEnumerable<(AllocationSourceKind Kind, int? WarehouseId, int? SupplierId, int Qty)>
        AllocationKey(OrderItemEditorState item) =>
        item.Allocations.Select(allocation =>
            (allocation.SourceKind, allocation.WarehouseId, allocation.SupplierId, allocation.Qty));

    private static string? ValidateAllocations(OrderItemEditorState item)
    {
        if (item.Allocations.Count == 0)
            return "يجب تحديد مصدر البند (مخزن أو مورد) قبل إضافته للطلب.";

        if (item.Allocations.Any(allocation => allocation.Qty <= 0))
            return "كميات المصدر يجب أن تكون أكبر من صفر.";

        if (item.Allocations.Any(allocation =>
                allocation.SourceKind == AllocationSourceKind.Warehouse && !allocation.WarehouseId.HasValue))
            return "حدد المخزن لكل كمية مخصصة.";

        var supplierAllocations = item.Allocations
            .Where(allocation => allocation.SourceKind == AllocationSourceKind.Supplier)
            .ToList();

        if (supplierAllocations.Count > 1)
            return "لا يمكن تخصيص كمية البند لأكثر من مورد واحد.";

        if (supplierAllocations.Count == 1)
        {
            if (!item.SupplierId.HasValue ||
                supplierAllocations[0].SupplierId != item.SupplierId)
                return "يجب تحديد المورد للكمية غير المتوفرة في المخازن.";

            if (!item.Suppliers.Any(supplier => supplier.Id == item.SupplierId))
                return "المورد المحدد غير متاح لهذا المنتج.";
        }

        if (item.Allocations.Sum(allocation => allocation.Qty) != item.Quantity)
            return "مجموع الكميات المخصصة يجب أن يساوي كمية البند.";

        return null;
    }

    private async Task RemoveItem(OrderItemEditorState item)
    {
        var result = await DialogService.ShowMessageBoxAsync("حذف منتج", "هل تريد حذف هذا المنتج من قائمة المنتجات المضافة؟", yesText: "نعم", cancelText: "لا");

        if (result is not true)
            return;

        if (!_disposed && Items.Remove(item))
        {
            await ItemsChanged.InvokeAsync();
            Snackbar.Add("تم حذف المنتج من القائمة");
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _addDialog?.Close(DialogResult.Cancel());
    }
}
