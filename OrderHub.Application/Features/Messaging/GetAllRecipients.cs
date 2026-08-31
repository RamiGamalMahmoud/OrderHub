using MediatR;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Messaging;

public static class GetAllRecipients
{
    public record Query : IRequest<IReadOnlyList<Recipient>>;

    public record Recipient(
        int RecipientId,
        string RecipientName,
        string Destination,
        RecipientType RecipientType);
}