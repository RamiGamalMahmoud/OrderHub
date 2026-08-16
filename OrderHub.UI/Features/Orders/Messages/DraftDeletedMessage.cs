using System;

namespace OrderHub.UI.Features.Orders.Messages;

public record DraftDeletedMessage(Guid DraftId);