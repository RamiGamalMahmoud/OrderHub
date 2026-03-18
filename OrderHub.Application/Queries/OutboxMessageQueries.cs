using MediatR;
using OrderHub.Domain.Models;
using System.Collections.Generic;

namespace OrderHub.Application.Queries;

public static class OutboxMessageQueries
{
    public record GetOutboxMessagesQuery() : IRequest<IEnumerable<OutboxMessage>>;
}
