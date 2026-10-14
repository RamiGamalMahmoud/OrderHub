using OrderHub.Application.Features.Orders.GetItemEditor;
using OrderHub.Application.Features.Orders.GetForEdit;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderHub.Application.Features.OrderDrafts.Save;
using OrderHub.Application.Features.OrderDrafts.Get;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Application.Features.Orders;

using OrderHub.Application.Features.Setup.Properties.Get;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Services.Pricing;
using OrderHub.Web.Components.Pages.Features.Orders.Editor.State;

namespace OrderHub.Web.Components.Pages.Features.Orders.Editor;

public partial class OrderEditor : IDisposable
{
    private bool _isDraftsOpen;
    private bool _isSavingUpdate;
    private string? _loadError;

    private OrderEditorStep _activeStep = OrderEditorStep.Products;

    private OrderEditorState _state = new();

    private IReadOnlyList<string> ValidationErrors => GetFinalValidationErrors(_state, IsEdit);

    private bool CanSave => _state.CanEdit && !_isSavingUpdate && !_draftLifecycle.IsClosed && ValidationErrors.Count == 0 &&
        (IsEdit || (!_draftLifecycle.IsCompleting && !_draftLifecycle.IsCompleted));

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
            .ToList());
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
        _activeStep = OrderEditorStep.Products;
        _isDraftsOpen = false;

        if (OrderId.HasValue)
        {
            _pageTitle = $"تعديل طلب {OrderId.Value}";

            // لاحقًا:
            _state = await LoadOrderAsync(OrderId.Value);

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
            return new OrderEditorState { IsArchived = true };
        }

        OrderEditorState state = new OrderEditorState()
        {
            Version = order.Version,
            Status = order.Status,
            IsArchived = order.IsArchived,
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

    private void SelectStep(OrderEditorStep step)
    {
        _activeStep = step;
    }

    private Variant GetStepVariant(OrderEditorStep step)
    {
        return _activeStep == step
            ? Variant.Filled
            : Variant.Text;
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
                item.Properties.Select(property => new OrderInput.Property(property.PropertyId, property.Value)).ToList())).ToList(),
            method == DeliveryMethod.DeliveryChain
                ? steps.Select((step, index) => new OrderInput.DeliveryStep(
                    preserveStepOrder ? step.StepOrder : index + 1, step.Method, step.HandlerId!.Value, IsEdit ? step.Id : null)).ToList()
                : new List<OrderInput.DeliveryStep>(),
            _state.PaymentMethodId);
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
        if (state.Delivery.Method.HasValue && !Enum.IsDefined(state.Delivery.Method.Value) ||
            state.OrderSource.HasValue && !Enum.IsDefined(state.OrderSource.Value))
            errors.Add("مصدر الطلب أو طريقة التوصيل غير صالح.");
        foreach (var item in state.Items)
        {
            if (item.SupplierId.HasValue && !(preservePersistedSnapshot && item.SupplierId == item.PersistedSupplierId) &&
                item.Suppliers.Count(supplier => supplier.Id == item.SupplierId) != 1)
                errors.Add($"{item.ProductName}: المورد المحدد غير متاح.");
        }
        if (activeState.Delivery.Steps.Any(step => step.Method is not (DeliveryMethod.DeliveryMan or DeliveryMethod.ShippingCompany)))
            errors.Add("طريقة تنفيذ خطوة التوصيل غير صالحة.");
        return errors;
    }

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
            var orderId = OrderId!.Value;
            try
            {
                var updateResult = await RequestExecutor!.ExecuteAsync(new Application.Features.Orders.Update.UpdateOrderCommand(orderId, input, _state.Version));
                if (updateResult.IsSuccess) _state.Version = updateResult.Value.Version;
                if (updateResult.IsSuccess && input.DeliveryMethod == DeliveryMethod.DeliveryChain)
                {
                    var saved = updateResult.Value;
                    if (saved is not null)
                    {
                        for (var index = 0; index < submittedStates.Length; index++)
                            submittedStates[index].Id = saved.DeliverySteps.Single(savedStep =>
                                savedStep.StepOrder == submittedSteps[index].StepOrder).Id;
                    }
                }
                else if (updateResult.IsSuccess)
                {
                    foreach (var step in submittedStates)
                        step.Id = null;
                }
                Snackbar!.Add(updateResult.IsSuccess ? "تم حفظ التعديلات بنجاح." : updateResult.ErrorMessage,
                    updateResult.IsSuccess ? Severity.Success : Severity.Error);
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

public enum OrderEditorStep
{
    Products,
    Details
}
