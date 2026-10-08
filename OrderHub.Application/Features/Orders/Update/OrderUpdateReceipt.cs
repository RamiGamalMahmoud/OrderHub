using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.Update;

public sealed record OrderUpdateReceipt(int Version, IReadOnlyList<SavedDeliveryStep> DeliverySteps);
public sealed record SavedDeliveryStep(int Id, int StepOrder);
