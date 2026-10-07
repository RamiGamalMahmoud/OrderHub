using OrderHub.Domain.Enums;

namespace OrderHub.Web.Components.Pages.Features.Orders.Editor.State;

public sealed class OrderEditorState
{
    public int Version { get; set; } = 1;
    public OrderStatus Status { get; set; } = OrderStatus.Processing;
    public bool IsArchived { get; set; }
    public bool CanEdit => !IsArchived && Status == OrderStatus.Processing;
    public int? ClientId { get; set; }

    public string? ClientName { get; set; }

    public int? PaymentMethodId { get; set; }

    public OrderSource? OrderSource { get; set; }

    public DeliveryEditorState Delivery { get; set; } = new();

    public List<OrderItemEditorState> Items { get; set; } = [];
}
