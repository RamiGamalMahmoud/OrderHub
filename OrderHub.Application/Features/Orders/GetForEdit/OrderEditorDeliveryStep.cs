using MediatR;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.GetForEdit;

public record OrderEditorDeliveryStep(
        int StepOrder,
        DeliveryMethod DeliveryMethod,
        int? HandlerId,
        int? Id = null);
