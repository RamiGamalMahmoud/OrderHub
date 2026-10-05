using OrderHub.Domain.Enums;

namespace OrderHub.Web.Components.Pages.Features.Orders.Editor.State;

public sealed class OrderEditorState
{
    public int? ClientId { get; set; }

    public int? PaymentMethodId { get; set; }

    public OrderSource? OrderSource { get; set; }

    public DeliveryEditorState Delivery { get; set; } = new();

    public List<OrderItemEditorState> Items { get; set; } = [];
}