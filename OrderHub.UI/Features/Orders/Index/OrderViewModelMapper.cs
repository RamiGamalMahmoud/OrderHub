using OrderHub.Application.Features.Orders.Queries;
using OrderHub.Application.Interfaces;

namespace OrderHub.UI.Features.Orders.Index;
internal static class OrderViewModelMapper
{
    public static OrderViewModel Map(
        GetOrderPaged.Order order,
        IRequestExecutor requestExecutor)
    {
        return new OrderViewModel(
            requestExecutor,
            order.Id,
            order.OrderNumber,
            order.ClientName,
            order.ClientPhoneNumber,
            order.ItemsCount,
            order.Total,
            order.CreatedAt,
            new PaymentMethod(
                order.PaymentMethod.Id,
                order.PaymentMethod.DisplayName,
                order.PaymentMethod.Description,
                order.PaymentMethod.IsActive),
            order.EnumItem,
            order.HasClientRecipient,
            order.HasSupplierRecipient,
            order.HasShippingCarrierRecipient,
            order.HasDeliverymanRecipient,
            order.IsClientMessageSent,
            order.IsSupplierMessageSent,
            order.IsShippingCarrierMessageSent,
            order.IsDeliverymanMessageSent);
    }
}
