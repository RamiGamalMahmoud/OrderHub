using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.Update;

public sealed record OrderUpdateReceipt(
    int Version,
    IReadOnlyList<SavedDeliveryStep> DeliverySteps,
    IReadOnlyList<SavedOrderItem> OrderItems = null,
    bool PendingApproval = false);

public sealed record SavedDeliveryStep(int Id, int StepOrder);

public sealed record SavedOrderItem(int Id, int ProductId);
