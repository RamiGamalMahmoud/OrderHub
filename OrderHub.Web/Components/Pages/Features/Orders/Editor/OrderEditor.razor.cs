using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderHub.Web.Components.Pages.Features.Orders.Editor.State;

namespace OrderHub.Web.Components.Pages.Features.Orders.Editor;

public partial class OrderEditor
{
    private bool _isDraftsOpen;

    private OrderEditorStep _activeStep = OrderEditorStep.Products;

    private OrderEditorState _state = new();

    private long _changeVersion;
    private (int? OrderId, Guid? DraftId)? _routeIdentity;
    private Guid _sessionId;

    private void OnEditorChanged() => _changeVersion++;

    [Parameter]
    public int? OrderId { get; set; }

    [Parameter]
    public Guid? DraftId { get; set; }

    private string _pageTitle = string.Empty;

    private bool IsEdit => OrderId.HasValue;

    private bool CanShowDrafts => !IsEdit;

    protected override Task OnParametersSetAsync()
    {
        var identity = (OrderId, DraftId: OrderId.HasValue ? null : DraftId);
        if (_routeIdentity == identity)
            return Task.CompletedTask;

        _routeIdentity = identity;
        _sessionId = Guid.NewGuid();
        _state = new OrderEditorState();
        _changeVersion = 0;
        _activeStep = OrderEditorStep.Products;
        _isDraftsOpen = false;

        if (OrderId.HasValue)
        {
            _pageTitle = $"تعديل طلب {OrderId.Value}";

            // لاحقًا:
            // _state = await LoadOrderAsync(OrderId.Value);

            return Task.CompletedTask;
        }

        if (DraftId.HasValue)
        {
            _pageTitle = "استكمال طلب";

            // لاحقًا:
            // _state = await LoadDraftAsync(DraftId.Value);

            return Task.CompletedTask;
        }

        _pageTitle = "إنشاء طلب جديد";

        return Task.CompletedTask;
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
}

public enum OrderEditorStep
{
    Products,
    Details
}
