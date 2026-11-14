using OrderHub.Domain.Enums;

namespace OrderHub.Web.Components.Pages.Features.Orders;

public sealed class OrdersListState
{
    public int Page { get; set; }
    public int PageSize { get; set; } = 10;

    public string? SearchTerm { get; set; }
    public OrderStatus? SelectedStatus { get; set; }
    public int? SelectedPaymentMethodId { get; set; }
}
