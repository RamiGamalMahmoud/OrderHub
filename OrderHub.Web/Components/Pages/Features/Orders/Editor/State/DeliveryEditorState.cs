using OrderHub.Domain.Enums;

namespace OrderHub.Web.Components.Pages.Features.Orders.Editor.State;

public sealed class DeliveryEditorState
{
    public DeliveryMethod? Method { get; set; }

    public int? DeliverymanId { get; set; }

    public int? ShippingCarrierId { get; set; }

    public List<DeliveryStepEditorState> Steps { get; set; } = [];
}