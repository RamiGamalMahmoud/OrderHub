using OrderHub.Domain.Enums;
using System;

namespace OrderHub.UI.Messaging;

public static class OrdersMessages
{
    public sealed record OrderCreatedMessage(int OrderId);

    public sealed record OrderUpdatedMessage(int OrderId);

    public sealed record OrderDeletedMessage(int OrderId);

    public sealed record OrderStatusChangedMessage(int OrderId, OrderStatus NewStatus);

    public sealed record OrderPaymentMethodChangedMessage(int OrderId, int PaymentMethodId);

    public sealed record OrderDraftSavedMessage(Guid DraftId);

    public sealed record OrderDraftDeletedMessage(Guid DraftId);
}
