using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Orders.NotifyOrderParticipants;

public record NotifyOrderParticipantsCommand(int OrderId, NotificationRecipient Recipient) : IRequest<Result>;

public record NotificationRecipient(
    RecipientType Type,
    int? EntityId = null);