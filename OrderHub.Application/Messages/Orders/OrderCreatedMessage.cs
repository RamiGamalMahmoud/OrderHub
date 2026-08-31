using OrderHub.Domain.Models;
using System.Collections.Generic;

namespace OrderHub.Application.Messages.Orders;

public record OrderCreatedMessage;
public record MessagesCreatedMessage(IEnumerable<OutboxMessage> OutboxMessages);
