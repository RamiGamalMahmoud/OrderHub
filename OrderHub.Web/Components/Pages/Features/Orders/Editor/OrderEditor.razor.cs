using OrderHub.Application.Features.Orders.GetItemEditor;
using OrderHub.Application.Features.Orders.GetForEdit;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderHub.Application.Features.OrderDrafts.Save;
using OrderHub.Application.Features.OrderDrafts.Get;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Application.Features.Orders.Update;
using OrderHub.Application.Features.Orders;
using OrderHub.Application.Features.Documents.ProformaInvoices;
using OrderHub.Application.Features.Documents.Quotations;
using System.Globalization;

using OrderHub.Application.Features.Setup.Properties.Get;
using OrderHub.Application.Authorization;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Services.Pricing;
using OrderHub.Web.Components.Pages.Features.Orders.Editor.State;

namespace OrderHub.Web.Components.Pages.Features.Orders.Editor;

public partial class OrderEditor : IDisposable
{
    private bool _isDraftsOpen;
    private bool _isSavingUpdate;
    private bool _isCreatingDocument;
    private bool _pendingApprovalSent;
    private string? _loadError;
    private string? _createdDocumentNumber;
    private DocumentType? _createdDocumentType;

    private OrderEditorState _state = new();

    private IReadOnlyList<string> ValidationErrors => GetFinalValidationErrors(_state, IsEdit);

    private bool CanSave => _state.CanEdit && !_isSavingUpdate && !_pendingApprovalSent && !_draftLifecycle.IsClosed && ValidationErrors.Count == 0 &&
        (IsEdit || (!_draftLifecycle.IsCompleting && !_draftLifecycle.IsCompleted));

    private bool CanCreateDraftDocument => !IsEdit && !_isCreatingDocument && !_draftLifecycle.IsCompleting &&
        !_draftLifecycle.IsClosed && CurrentPricing is not null && _state.ClientId is > 0;

    private long _changeVersion;
    private (int? OrderId, Guid? DraftId)? _routeIdentity;
    private Guid _sessionId;
    private DraftLifecycle _draftLifecycle = new();

    private sealed class DraftLifecycle
    {
        public Guid? PersistedDraftId { get; set; }
        public bool IsCompleting { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsClosed { get; set; }
        public HashSet<Task> PendingWrites { get; } = [];
    }

    private async Task OnEditorChanged()
    {
        if (_draftLifecycle.IsClosed || _draftLifecycle.IsCompleted || _draftLifecycle.IsCompleting)
            return;
        _changeVersion++;

        if (OrderId.HasValue)
            return;

        await SaveDraft();
    }

    private async Task SaveDraft()
    {
        var lifecycle = _draftLifecycle;
        if (IsEdit || lifecycle.IsClosed || lifecycle.IsCompleted || lifecycle.IsCompleting)
            return;
        OrderDraftData orderDraftData = new OrderDraftData(
            _state.ClientId,
            _state.ClientName ?? "",
            _state.OrderSource,
            _state.PaymentMethodId,
            _state.Delivery.Method,
            _state.Delivery.DeliverymanId,
            _state.Delivery.ShippingCarrierId,
            _state.Items.Select(item => new OrderDraftItem(
                item.ProductId,
                item.ProductName,
                item.CategoryName,
                item.UnitPrice,
                item.Quantity,
                item.VatRate,
                item.SupplierId,
                "",
                item.Properties.Select(property => new OrderDraftProperty(
                    property.PropertyId,
                    property.Value))
                .ToList()))
            .ToList(),
            _state.Delivery.Steps.Select(step => new OrderDraftDeliveryStep(
                step.StepOrder,
                step.Method,
                step.HandlerId))
            .ToList(), _state.DeliveryCost, _state.DiscountType, _state.DiscountValue);
        var draft = new SaveDraftCommand(_sessionId, orderDraftData);
        var write = PersistDraftAsync(lifecycle, draft);
        lifecycle.PendingWrites.Add(write);
        try
        {
            await write;
        }
        finally
        {
            lifecycle.PendingWrites.Remove(write);
        }
    }

    private async Task PersistDraftAsync(DraftLifecycle lifecycle, SaveDraftCommand draft)
    {
        var result = await RequestExecutor!.ExecuteAsync(draft);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.ErrorMessage);
        lifecycle.PersistedDraftId = draft.DraftId;
    }

    [Parameter]
    public int? OrderId { get; set; }

    [Inject]
    private IRequestExecutor? RequestExecutor { get; set; }

    [Inject]
    private ISnackbar? Snackbar { get; set; }

    [Inject]
    private IDialogService? Dialogs { get; set; }

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ILogger<OrderEditor> Logger { get; set; } = null!;

    [Parameter]
    public Guid? DraftId { get; set; }

    private string _pageTitle = string.Empty;

    private bool IsEdit => OrderId.HasValue;

    private bool CanShowDrafts => !IsEdit;

    private PricingResult? CurrentPricing => _state.Items.Count == 0 || _state.Items.Any(item =>
        item.Quantity <= 0 || item.UnitPrice <= 0 || item.VatRate is < 0 or > 100)
        ? null
        : PricingCalculator.Calculate(_state.Items.Select(item => new PricingItem(
            item.ProductId, item.ProductName, item.Quantity, item.UnitPrice, item.VatRate)).ToList());

    private decimal EditorDiscountAmount(PricingResult pricing) => _state.DiscountType == DiscountType.Percentage
        ? pricing.Subtotal * _state.DiscountValue / 100m : _state.DiscountValue;
    private decimal EditorFinalTotal(PricingResult pricing) => pricing.TotalAmount - EditorDiscountAmount(pricing) + _state.DeliveryCost;

    private Task CreateQuotationAsync() => CreateDraftDocumentAsync(DocumentType.Quotation);

    private Task CreateProformaInvoiceAsync() => CreateDraftDocumentAsync(DocumentType.ProformaInvoice);

    private async Task CreateDraftDocumentAsync(DocumentType type)
    {
        if (!CanCreateDraftDocument)
            return;

        _isCreatingDocument = true;
        _createdDocumentNumber = null;
        try
        {
            await SaveDraft();
            _createdDocumentNumber = type switch
            {
                DocumentType.Quotation => await RequestExecutor!.ExecuteAsync(
                    new CreateQuotationCommand.Command(_sessionId)),
                DocumentType.ProformaInvoice => await RequestExecutor!.ExecuteAsync(
                    new CreateProformaInvoiceCommand.Command(_sessionId)),
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
            _createdDocumentType = type;
            Snackbar?.Add($"تم إنشاء الوثيقة {_createdDocumentNumber}.", Severity.Success);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to create {DocumentType} for order draft {DraftId}.", type, _sessionId);
            Snackbar?.Add("تعذر إنشاء الوثيقة. تحقق من بيانات العميل والمنتجات ثم أعد المحاولة.", Severity.Error);
        }
        finally
        {
            _isCreatingDocument = false;
        }
    }

    private static string DocumentFileUrl(DocumentType type, string number)
    {
        var route = type switch
        {
            DocumentType.Quotation => "quotations",
            DocumentType.ProformaInvoice => "proforma-invoices",
            DocumentType.Invoice => "invoices",
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
        return $"/files/{route}/{Uri.EscapeDataString(number)}.pdf";
    }

    protected override async Task OnParametersSetAsync()
    {
        var identity = (OrderId, DraftId: OrderId.HasValue ? null : DraftId);
        if (_routeIdentity == identity)
            return;

        _routeIdentity = identity;
        _loadError = null;
        _draftLifecycle.IsClosed = true;
        _draftLifecycle = new DraftLifecycle();
        _state = new OrderEditorState();
        _changeVersion = 0;
        _createdDocumentNumber = null;
        _createdDocumentType = null;
        _isDraftsOpen = false;

        if (OrderId.HasValue)
        {
            _pageTitle = $"تعديل طلب #{OrderId.Value.ToString(CultureInfo.InvariantCulture)}";

            _state = await LoadOrderAsync(OrderId.Value);

            if (_state.OrderNumber is { } orderNumber)
                _pageTitle = $"تعديل طلب #{orderNumber}";

            return;
        }

        if (DraftId.HasValue)
        {
            var draftId = DraftId.Value;
            var lifecycle = _draftLifecycle;
            _sessionId = draftId;
            _pageTitle = "استكمال طلب";

            // لاحقًا:
            _state = await LoadDraftAsync(draftId);
            lifecycle.PersistedDraftId = draftId;

            return;
        }
        _sessionId = Guid.NewGuid();

        _pageTitle = "إنشاء طلب جديد";
    }

    private async Task<OrderEditorState> LoadDraftAsync(Guid value)
    {
        var draft = await RequestExecutor!.ExecuteAsync(new GetDraftQuery(value));

        OrderDraftData order = draft.Data;

        OrderEditorState state = new OrderEditorState()
        {
            ClientId = order.ClientId,
            ClientName = string.IsNullOrWhiteSpace(order.ClientName) ? null : order.ClientName,
            Delivery = new DeliveryEditorState()
            {
                DeliverymanId = order.DeliverymanId,
                ShippingCarrierId = order.ShippingCarrierId,
                Method = order.DeliveryMethod,
                Steps = order.DeliverySteps.OrderBy(step => step.StepOrder).Select(step => new DeliveryStepEditorState()
                {
                    HandlerId = step.HandlerId,
                    Method = step.DeliveryMethod,
                    StepOrder = step.StepOrder
                }).ToList()
            },
            OrderSource = order.OrderSource,
            PaymentMethodId = order.PaymentMethodId,
            DeliveryCost = order.DeliveryCost,
            DiscountType = order.DiscountType,
            DiscountValue = order.DiscountValue,
        };

        var products = new Dictionary<int, OrderItemEditorData?>();
        var historicalProperties = new Dictionary<int, PropertyDetails?>();
        foreach (var item in order.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
            {
                try
                {
                    product = await RequestExecutor!.ExecuteAsync(new GetOrderItemEditorQuery(item.ProductId));
                }
                catch (Exception)
                {
                    // Preserve the draft even when catalog metadata cannot currently be read.
                    product = null;
                }
                products[item.ProductId] = product;
            }
            var definitions = product?.Properties.ToList() ?? [];
            var properties = definitions.Select(definition => RestoreProperty(new OrderEditorProperty(
                definition.Id, definition.Name, definition.IsRequired, definition.PropertyType,
                definition.Options.Select(option => new OrderEditorOption(option.Id, option.Name)),
                item.Properties.FirstOrDefault(saved => saved.PropertyId == definition.Id)?.Value))).ToList();

            foreach (var saved in item.Properties.Where(saved => !definitions.Any(current => current.Id == saved.PropertyId)))
            {
                if (!historicalProperties.TryGetValue(saved.PropertyId, out var metadata))
                {
                    try
                    {
                        metadata = await RequestExecutor!.ExecuteAsync(new GetPropertyQuery(saved.PropertyId));
                    }
                    catch (Exception)
                    {
                        metadata = null;
                    }
                    historicalProperties[saved.PropertyId] = metadata;
                }
                properties.Add(metadata is null
                    ? new OrderItemPropertyEditorState
                    {
                        PropertyId = saved.PropertyId,
                        Name = "",
                        Value = saved.Value,
                        HasMetadata = false
                    }
                    : RestoreProperty(new OrderEditorProperty(
                        metadata.Id, metadata.Name,
                        // As in persisted-order restoration, removed associations have no current requirement.
                        false, metadata.PropertyType,
                        metadata.Options.Select(option => new OrderEditorOption(option.Id, option.Value)), saved.Value)));
            }

            state.Items.Add(new OrderItemEditorState
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                CategoryName = item.CategoryName,
                UnitPrice = item.Price,
                Quantity = item.Quantity,
                VatRate = item.VAT,
                SupplierId = item.SupplierId,
                MetadataRestorationError = product is null
                    ? "تعذر تحميل بيانات المنتج. بيانات المسودة محفوظة؛ أعد فتحها بعد استعادة المنتج أو احذف المنتج وأضفه من جديد."
                    : null,
                Suppliers = product?.Suppliers.Select(supplier => new OrderItemSupplierEditorState(
                    supplier.Id, supplier.Name)).ToList() ?? [],
                Properties = properties
            });
        }

        return state;
    }

    private async Task<OrderEditorState> LoadOrderAsync(int value)
    {
        var order = await RequestExecutor!.ExecuteAsync(new Application.Features.Orders.GetForEdit.GetOrderForEditQuery(value));
        if (order is null)
        {
            _loadError = "الطلب غير موجود.";
            return new OrderEditorState();
        }

        OrderEditorState state = new OrderEditorState()
        {
            Version = order.Version,
            Status = order.Status,
            OrderNumber = order.OrderNumber,
            ModifiedAt = order.ModifiedAt,
            DeliveryCost = order.DeliveryCost,
            DiscountType = order.DiscountType,
            DiscountValue = order.DiscountValue,
            ClientId = order.ClientId,
            OrderSource = order.OrderSource,
            PaymentMethodId = order.PaymentMothodId,
            Delivery = new DeliveryEditorState
            {
                Method = order.DeliveryMethod,
                DeliverymanId = order.DeliveryManId,
                ShippingCarrierId = order.ShippingCarrierId,
                Steps = order.DeliverySteps.OrderBy(step => step.StepOrder)
                    .Select(step => new DeliveryStepEditorState
                    {
                        Id = step.Id,
                        StepOrder = step.StepOrder,
                        Method = step.DeliveryMethod,
                        HandlerId = step.HandlerId
                    }).ToList()
            },
            Items = order.OrderItems.Select(item => new OrderItemEditorState()
            {
                ProductId = item.ProductId,
                ItemId = item.Id,
                CategoryName = item.CategoryName,
                ProductName = item.ProductName,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                VatRate = item.VatRate,
                SupplierId = item.SupplierId,
                PersistedSupplierId = item.SupplierId,
                SupplierName = item.SupplierName,
                // Preserve unavailable selections; the drawer already warns when the ID is absent.
                Suppliers = item.Suppliers.Select(supplier => new OrderItemSupplierEditorState(
                    supplier.Id, supplier.Name)).ToList(),
                Properties = item.Properties.Select(RestoreProperty).ToList(),
                Allocations = (item.Allocations ?? []).Select(allocation => new OrderItemAllocationEditorState
                {
                    SourceKind = allocation.SourceKind,
                    Qty = allocation.Qty,
                    WarehouseId = allocation.WarehouseId,
                    WarehouseName = allocation.WarehouseName,
                    SupplierId = allocation.SupplierId,
                    SupplierName = allocation.SupplierName
                }).ToList(),
            }).ToList()
        };

        return state;
    }

    private void ShowDrafts()
    {
        _isDraftsOpen = true;
    }

    private static OrderItemPropertyEditorState RestoreProperty(OrderEditorProperty property) => new()
    {
        PropertyId = property.PropertyId,
        Name = property.Name,
        IsRequired = property.IsRequired,
        Type = property.PropertyType,
        Value = property.SelectedValue,
        Options = property.Options.Select(option => new OrderItemPropertyOptionEditorState(
            option.OptionId, option.Value)).ToList()
    };

    private string? ModifiedText => _state.ModifiedAt is { } modified
        ? $"تم التحديث: {FormatRelative(modified)}"
        : null;

    private static string FormatRelative(DateTime value)
    {
        var now = value.Kind == DateTimeKind.Utc ? DateTime.UtcNow : DateTime.Now;
        var elapsed = now - value;

        if (elapsed < TimeSpan.FromMinutes(1))
            return "الآن";

        if (elapsed < TimeSpan.FromHours(1))
        {
            var minutes = (int)elapsed.TotalMinutes;
            return minutes == 1 ? "منذ دقيقة" : $"منذ {minutes.ToString(CultureInfo.InvariantCulture)} دقيقة";
        }

        if (elapsed < TimeSpan.FromDays(1))
        {
            var hours = (int)elapsed.TotalHours;
            return hours == 1 ? "منذ ساعة" : $"منذ {hours.ToString(CultureInfo.InvariantCulture)} ساعة";
        }

        var days = (int)elapsed.TotalDays;
        return days == 1 ? "منذ يوم" : $"منذ {days.ToString(CultureInfo.InvariantCulture)} يوم";
    }

    private OrderInput.Order BuildOrderInput()
    {
        var method = _state.Delivery.Method!.Value;
        var steps = _state.Delivery.Steps;
        var preserveStepOrder = steps.All(step => step.StepOrder > 0) &&
            steps.Zip(steps.Skip(1), (previous, next) => previous.StepOrder < next.StepOrder).All(inOrder => inOrder);
        return new OrderInput.Order(_state.ClientId!.Value, _state.OrderSource!.Value, method,
            method == DeliveryMethod.DeliveryMan ? _state.Delivery.DeliverymanId : null,
            method == DeliveryMethod.ShippingCompany ? _state.Delivery.ShippingCarrierId : null,
            _state.Items.Select(item => new OrderInput.Item(item.ProductId, item.ProductName, item.Quantity,
                item.UnitPrice, item.VatRate, ResolveSupplierName(item, IsEdit), item.SupplierId,
                item.Properties.Select(property => new OrderInput.Property(property.PropertyId, property.Value)).ToList(),
                item.ItemId,
                item.Allocations.Select(allocation => new OrderInput.Allocation(
                    allocation.SourceKind, allocation.Qty, allocation.WarehouseId, allocation.SupplierId)).ToList())).ToList(),
            method == DeliveryMethod.DeliveryChain
                ? steps.Select((step, index) => new OrderInput.DeliveryStep(
                    preserveStepOrder ? step.StepOrder : index + 1, step.Method, step.HandlerId!.Value, IsEdit ? step.Id : null)).ToList()
                : new List<OrderInput.DeliveryStep>(),
            _state.PaymentMethodId, _state.DeliveryCost, _state.DiscountType, _state.DiscountValue);
    }

    private static string? ResolveSupplierName(OrderItemEditorState item, bool preservePersistedSnapshot)
    {
        if (!item.SupplierId.HasValue)
            return null;
        if (preservePersistedSnapshot && item.SupplierId == item.PersistedSupplierId)
            return item.SupplierName;
        return item.Suppliers.FirstOrDefault(supplier => supplier.Id == item.SupplierId)?.Name;
    }

    private static IReadOnlyList<string> GetUpdateValidationErrors(OrderEditorState state)
        => GetFinalValidationErrors(state, true);

    private static IReadOnlyList<string> GetFinalValidationErrors(OrderEditorState state, bool preservePersistedSnapshot)
    {
        // Inactive steps are convenience state, not part of the final input.
        var activeState = new OrderEditorState
        {
            ClientId = state.ClientId,
            OrderSource = state.OrderSource,
            PaymentMethodId = state.PaymentMethodId,
            Items = state.Items,
            Delivery = new DeliveryEditorState
            {
                Method = state.Delivery.Method,
                DeliverymanId = state.Delivery.DeliverymanId,
                ShippingCarrierId = state.Delivery.ShippingCarrierId,
                Steps = state.Delivery.Method == DeliveryMethod.DeliveryChain ? state.Delivery.Steps : []
            }
        };
        var errors = GetValidationErrors(activeState).ToList();
        if (state.DeliveryCost < 0) errors.Add("تكلفة التوصيل لا يمكن أن تكون سالبة.");
        if (state.DiscountValue < 0) errors.Add("قيمة الخصم لا يمكن أن تكون سالبة.");
        if (state.DiscountType == DiscountType.Percentage && state.DiscountValue > 100) errors.Add("نسبة الخصم يجب ألا تتجاوز 100%.");
        if (state.Items.Count == 0 && state.DiscountValue > 0) errors.Add("لا يمكن تطبيق خصم دون منتجات.");
        if (CurrentPricingFor(state) is { } pricing)
        {
            var amount = state.DiscountType == DiscountType.Percentage ? pricing.Subtotal * state.DiscountValue / 100m : state.DiscountValue;
            if (state.DiscountType == DiscountType.FixedAmount && state.DiscountValue > pricing.Subtotal) errors.Add("الخصم لا يمكن أن يتجاوز إجمالي المنتجات قبل الضريبة.");
            if (amount > pricing.Subtotal || pricing.TotalAmount - amount + state.DeliveryCost < 0) errors.Add("قيمة الخصم تؤدي إلى إجمالي غير صالح.");
        }
        if (state.Delivery.Method.HasValue && !Enum.IsDefined(state.Delivery.Method.Value) ||
            state.OrderSource.HasValue && !Enum.IsDefined(state.OrderSource.Value))
            errors.Add("مصدر الطلب أو طريقة التوصيل غير صالح.");
        foreach (var item in state.Items)
        {
            if (item.Allocations.Count == 0)
            {
                errors.Add($"{item.ProductName}: يجب تحديد مصدر البند (مخزن أو مورد) قبل إضافته للطلب.");
                continue;
            }

            if (item.Allocations.Sum(allocation => allocation.Qty) != item.Quantity)
                errors.Add($"{item.ProductName}: مجموع الكميات المخصصة يجب أن يساوي كمية البند.");

            if (!item.Allocations.Any(allocation => allocation.SourceKind == AllocationSourceKind.Supplier))
                continue;

            if (!item.SupplierId.HasValue)
            {
                errors.Add($"{item.ProductName}: يجب تحديد المورد للكمية غير المتوفرة في المخازن.");
            }
            else if (!(preservePersistedSnapshot && item.SupplierId == item.PersistedSupplierId) &&
                item.Suppliers.Count(supplier => supplier.Id == item.SupplierId) != 1)
                errors.Add($"{item.ProductName}: المورد المحدد غير متاح.");
        }
        if (activeState.Delivery.Steps.Any(step => step.Method is not (DeliveryMethod.DeliveryMan or DeliveryMethod.ShippingCompany)))
            errors.Add("طريقة تنفيذ خطوة التوصيل غير صالحة.");
        return errors;
    }

    private static PricingResult? CurrentPricingFor(OrderEditorState state) => state.Items.Count == 0 || state.Items.Any(item => item.Quantity <= 0 || item.UnitPrice <= 0 || item.VatRate is < 0 or > 100)
        ? null : PricingCalculator.Calculate(state.Items.Select(item => new PricingItem(item.ProductId, item.ProductName, item.Quantity, item.UnitPrice, item.VatRate)).ToList());

    private static IReadOnlyList<string> GetValidationErrors(OrderEditorState state)
    {
        List<string> errors = [];

        if (!state.ClientId.HasValue)
        {
            errors.Add("يجب اختيار العميل.");
        }

        if (!state.OrderSource.HasValue)
        {
            errors.Add("يجب تحديد مصدر الطلب.");
        }

        if (!state.Delivery.Method.HasValue)
        {
            errors.Add("يجب تحديد آلية التوصيل.");
        }
        else
        {
            switch (state.Delivery.Method.Value)
            {
                case DeliveryMethod.DeliveryMan:
                    if (!state.Delivery.DeliverymanId.HasValue)
                    {
                        errors.Add("يجب اختيار مندوب التوصيل.");
                    }
                    break;

                case DeliveryMethod.ShippingCompany:
                    if (!state.Delivery.ShippingCarrierId.HasValue)
                    {
                        errors.Add("يجب اختيار شركة الشحن.");
                    }
                    break;
            }
        }

        if (state.Items.Count == 0)
        {
            errors.Add("يجب إضافة منتج واحد على الأقل.");
        }

        if (state.Items.Any(item => item.UnitPrice <= 0))
        {
            errors.Add("يجب أن يكون سعر الوحدة اكبر من الصفر.");
        }

        if (state.Items.Any(item => item.Quantity <= 0))
        {
            errors.Add("يجب أن تكون الكمية أكبر من الصفر.");
        }

        foreach (var item in state.Items)
        {
            if (item.MetadataRestorationError is { } metadataError)
                errors.Add($"{item.ProductName} - {metadataError}");

            foreach (var property in item.Properties)
            {
                string? error = property.ValidateValue(property.Value);

                if (error is not null)
                {
                    errors.Add($"{item.ProductName} - {error}");
                }
            }
        }

        foreach (var step in state.Delivery.Steps)
        {
            if (!step.HandlerId.HasValue)
            {
                errors.Add(
                    $"خطوة التوصيل رقم {step.StepOrder}: يجب تحديد جهة تنفيذ الخطوة.");
            }
        }

        return errors;
    }

    private async Task<OrderUpdateReceipt?> RequestApprovalAsync(int orderId, OrderInput.Order input)
    {
        if (Dialogs is null)
            return null;

        try
        {
            DialogParameters<OrderEditApprovalDialog> parameters = new()
            {
                {
                    dialog => dialog.Submit,
                    reason => RequestExecutor!.ExecuteAsync(
                        new Application.Features.Orders.Update.UpdateOrderCommand(orderId, input, _state.Version, reason))
                }
            };
            DialogOptions options = new()
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                CloseButton = false,
                CloseOnEscapeKey = false,
                BackdropClick = false
            };

            var dialog = await Dialogs.ShowAsync<OrderEditApprovalDialog>(
                "إرسال التعديل للاعتماد",
                parameters,
                options);
            DialogResult? result = await dialog.Result;
            return result is { Canceled: false, Data: OrderUpdateReceipt receipt } ? receipt : null;
        }
        catch (AuthorizationDeniedException)
        {
            Navigation.NavigateTo("/access-denied");
            return null;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to queue an approval request for order {OrderId}", orderId);
            Snackbar!.Add("تعذر فتح نافذة الاعتماد. يرجى المحاولة مرة أخرى.", Severity.Error);
            return null;
        }
    }

    private async Task SaveAsync()
    {
        if (!CanSave)
            return;

        OrderInput.Order input = BuildOrderInput();


        if (IsEdit)
        {
            _isSavingUpdate = true;
            var submittedStates = _state.Delivery.Steps.ToArray();
            var submittedSteps = input.DeliverySteps.ToList();
            var submittedItems = _state.Items.ToArray();
            var orderId = OrderId!.Value;
            try
            {
                var updateResult = await RequestExecutor!.ExecuteAsync(new Application.Features.Orders.Update.UpdateOrderCommand(orderId, input, _state.Version));

                if (!updateResult.IsSuccess && updateResult.ErrorCode == "ApprovalRequired")
                {
                    OrderUpdateReceipt? queued = await RequestApprovalAsync(orderId, input);
                    if (queued is null)
                        return;

                    if (queued.Version > 0)
                        _state.Version = queued.Version;
                    _pendingApprovalSent = true;
                    Snackbar!.Add("تم إرسال التعديل إلى المدير للاعتماد.", Severity.Success);
                    return;
                }

                if (updateResult.IsSuccess)
                {
                    OrderUpdateReceipt saved = updateResult.Value;
                    _state.Version = saved.Version;
                    if (saved.OrderItems is { } savedItems)
                    {
                        for (var index = 0; index < submittedItems.Length && index < savedItems.Count; index++)
                        {
                            if (savedItems[index].ProductId == submittedItems[index].ProductId)
                                submittedItems[index].ItemId = savedItems[index].Id;
                        }
                    }

                    if (input.DeliveryMethod == DeliveryMethod.DeliveryChain)
                    {
                        for (var index = 0; index < submittedStates.Length; index++)
                            submittedStates[index].Id = saved.DeliverySteps.Single(savedStep =>
                                savedStep.StepOrder == submittedSteps[index].StepOrder).Id;
                    }
                    else
                    {
                        foreach (var step in submittedStates)
                            step.Id = null;
                    }

                    Snackbar!.Add("تم حفظ التعديلات بنجاح.", Severity.Success);
                }
                else
                {
                    Snackbar!.Add(updateResult.ErrorMessage, Severity.Error);
                }
            }
            finally
            {
                _isSavingUpdate = false;
            }
            return;
        }

        var lifecycle = _draftLifecycle;
        lifecycle.IsCompleting = true;
        try
        {
            // Drain writes already running before consuming their draft. No ordering between
            // ordinary autosaves is imposed; new writes are blocked only during completion.
            await Task.WhenAll(lifecycle.PendingWrites.ToArray());
            if (lifecycle.IsClosed)
                return;
            var result = await RequestExecutor!.ExecuteAsync(new Application.Features.Orders.Create.CreateOrderCommand(
                lifecycle.PersistedDraftId, input, _sessionId == Guid.Empty ? null : _sessionId));
            if (result.IsSuccess)
                lifecycle.IsCompleted = true;
            if (!lifecycle.IsClosed)
                Snackbar!.Add(result.IsSuccess ? "تم إنشاء الطلب بنجاح." : result.ErrorMessage,
                    result.IsSuccess ? Severity.Success : Severity.Error);
        }
        catch (Exception exception)
        {
            if (!lifecycle.IsClosed)
                Snackbar!.Add(exception.Message, Severity.Error);
        }
        finally
        {
            lifecycle.IsCompleting = false;
        }
    }

    public void Dispose() => _draftLifecycle.IsClosed = true;
}
