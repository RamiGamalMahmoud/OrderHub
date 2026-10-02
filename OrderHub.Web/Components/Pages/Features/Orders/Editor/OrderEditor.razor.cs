using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderHub.Web.Components.Pages.Features.Orders.Editor.State;

namespace OrderHub.Web.Components.Pages.Features.Orders.Editor;

public partial class OrderEditor
{
    private bool _isDraftsOpen;

    private OrderEditorStep _activeStep = OrderEditorStep.Products;

    private OrderEditorState _state = new();

    [Parameter]
    public int? OrderId { get; set; }

    [Parameter]
    public Guid? DraftId { get; set; }

    private string _pageTitle = string.Empty;

    private bool IsEdit => OrderId.HasValue;

    private bool CanShowDrafts => !IsEdit;

    protected override async Task OnParametersSetAsync()
    {
        if (OrderId.HasValue)
        {
            _pageTitle = $"تعديل طلب {OrderId.Value}";

            // لاحقًا:
            // _state = await LoadOrderAsync(OrderId.Value);

            return;
        }

        if (DraftId.HasValue)
        {
            _pageTitle = "استكمال طلب";

            // لاحقًا:
            // _state = await LoadDraftAsync(DraftId.Value);

            return;
        }

        _pageTitle = "إنشاء طلب جديد";

        await InitializeNewOrderAsync();
    }

    private Task InitializeNewOrderAsync()
    {
        _state = new OrderEditorState();

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