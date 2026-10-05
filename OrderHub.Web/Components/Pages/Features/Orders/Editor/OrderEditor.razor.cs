using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderHub.Application.Context;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Enums;
using OrderHub.Web.Components.Pages.Features.Orders.Editor.State;

namespace OrderHub.Web.Components.Pages.Features.Orders.Editor;

public partial class OrderEditor
{
    private bool _isDraftsOpen;

    private OrderEditorStep _activeStep = OrderEditorStep.Products;

    private OrderEditorState _state = new();

    private IReadOnlyList<string> ValidationErrors => GetValidationErrors(_state);

    private bool CanSave => ValidationErrors.Count == 0;

    private long _changeVersion;
    private (int? OrderId, Guid? DraftId)? _routeIdentity;
    private Guid _sessionId;

    private async Task OnEditorChanged()
    {
        _changeVersion++;

        if (OrderId.HasValue)
            return;

        await SaveDraft();
    }

    private async Task SaveDraft()
    {
        OrderDraftData orderDraftData = new OrderDraftData(
            _state.ClientId,
            "",
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
        await DraftStore.SaveAsync(new Draft(_sessionId,
            _userId.Value,
            DateTime.UtcNow,
            orderDraftData));
    }

    [Parameter]
    public int? OrderId { get; set; }

    [Inject]
    private IRequestExecutor? RequestExecutor { get; set; }

    [Inject]
    private IDraftStore? DraftStore { get; set; }

    [Inject]
    private ISnackbar? Snackbar { get; set; }

    [Inject]
    private ICurrentUser? CurrentUser { get; set; }

    [Parameter]
    public Guid? DraftId { get; set; }

    private string _pageTitle = string.Empty;
    private int? _userId;

    private bool IsEdit => OrderId.HasValue;

    private bool CanShowDrafts => !IsEdit;

    protected override async Task OnInitializedAsync()
    {
        _userId = await CurrentUser!.GetUserIdAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        var identity = (OrderId, DraftId: OrderId.HasValue ? null : DraftId);
        if (_routeIdentity == identity)
            return;

        _routeIdentity = identity;
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
            _sessionId = DraftId.Value;
            _pageTitle = "استكمال طلب";

            // لاحقًا:
            _state = await LoadDraftAsync(DraftId.Value);

            return;
        }
        _sessionId = Guid.NewGuid();

        _pageTitle = "إنشاء طلب جديد";
    }

    private async Task<OrderEditorState> LoadDraftAsync(Guid value)
    {
        int? userId = await CurrentUser!.GetUserIdAsync();
        var draft = await DraftStore!.GetAsync(value, userId.Value);

        OrderDraftData order = draft.Data;

        OrderEditorState state = new OrderEditorState()
        {
            ClientId = order.ClientId,
            Items = order.Items.Select(draftItem => new OrderItemEditorState()
            {
                ProductId = draftItem.ProductId,
                CategoryName = draftItem.CategoryName,
                ProductName = draftItem.ProductName,
                UnitPrice = draftItem.Price,
                Quantity = draftItem.Quantity,
                VatRate = draftItem.VAT,
                SupplierId = draftItem.SupplierId,
                Properties = draftItem.Properties.Select(property => new OrderItemPropertyEditorState()
                {
                  PropertyId = property.PropertyId,
                  Value = property.Value,
                  Name = ""
                })
                .ToList()
            })
            .ToList(),
            Delivery = new DeliveryEditorState()
            {
                DeliverymanId = order.DeliverymanId,
                ShippingCarrierId = order.ShippingCarrierId,
                Method = order.DeliveryMethod,
                Steps = order.DeliverySteps.Select(step => new DeliveryStepEditorState()
                {
                    HandlerId = step.HandlerId,
                    Method = step.DeliveryMethod,
                    StepOrder = step.StepOrder
                }).ToList()
            },
            OrderSource = order.OrderSource,
            PaymentMethodId = order.PaymentMethodId,
        };

        return state;
    }

    private async Task<OrderEditorState> LoadOrderAsync(int value)
    {
        var order = await RequestExecutor!.ExecuteAsync(new Application.Features.Orders.Queries.GetOrderEdit.Query(value));

        OrderEditorState state = new OrderEditorState()
        {
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
                // Preserve unavailable selections; the drawer already warns when the ID is absent.
                Suppliers = item.Suppliers.Select(supplier => new OrderItemSupplierEditorState(
                    supplier.Id, supplier.Name)).ToList(),
                Properties = item.Properties.Select(property => new OrderItemPropertyEditorState
                {
                    PropertyId = property.PropertyId,
                    Name = property.Name,
                    IsRequired = property.IsRequired,
                    Type = property.PropertyType,
                    Value = property.SelectedValue,
                    Options = property.Options.Select(option => new OrderItemPropertyOptionEditorState(
                        option.OptionId, option.Value)).ToList()
                }).ToList(),
            }).ToList()
        };

        return state;
    }

    private void ShowDrafts()
    {
        _isDraftsOpen = true;
    }

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
        return new OrderInput.Order(
            _state.ClientId!.Value,
            _state.OrderSource!.Value,
            _state.Delivery.Method!.Value,
            _state.Delivery.DeliverymanId,
            _state.Delivery.ShippingCarrierId,
            _state.Items.Select(item => new OrderInput.Item(
                item.ProductId,
                item.ProductName,
                item.Quantity,
                item.UnitPrice,
                item.VatRate,
                item.SupplierId.HasValue ? item.Suppliers.Single(s => s.Id == item.SupplierId).Name : null,
                item.SupplierId,
                item.Properties.Select(property => new OrderInput.Property(
                    property.PropertyId,
                    property.Value)))),
            _state.Delivery.Steps.Select(step => new OrderInput.DeliveryStep(
                step.StepOrder,
                step.Method,
                step.HandlerId!.Value)),
            _state.PaymentMethodId);
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
            // Update
            await RequestExecutor!.ExecuteAsync(new Application.Features.Orders.Update.UpdateOrderCommand(OrderId!.Value, input));
            return;
        }

        // Create
        var result = await RequestExecutor!.ExecuteAsync(new Application.Features.Orders.Create.CreateOrderCommand(DraftId, input));
        if (result.IsSuccess)
        {
            Snackbar!.Add("تم إنشاء الطلب بنجاح.", Severity.Success);
        }
        else
        {
            Snackbar!.Add(result.ErrorMessage, Severity.Error);
        }
    }
}

public enum OrderEditorStep
{
    Products,
    Details
}
