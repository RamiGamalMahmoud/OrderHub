using System;
using OrderHub.Domain.Enums;

namespace OrderHub.Domain.Models;

public sealed record OrderOperation(int UserId, string UserName, DateTime AtUtc);

public sealed record DeliveryStepOutcome(int StepId, DeliveryStatus Status, string Reason = null);
