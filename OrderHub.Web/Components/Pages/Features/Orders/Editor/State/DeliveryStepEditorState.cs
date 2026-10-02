using OrderHub.Domain.Enums;

namespace OrderHub.Web.Components.Pages.Features.Orders.Editor.State;

public sealed class DeliveryStepEditorState
{
    public DeliveryMethod Method { get; set; }

    public int? HandlerId { get; set; }
}