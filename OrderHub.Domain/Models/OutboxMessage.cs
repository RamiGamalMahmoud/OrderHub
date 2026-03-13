using OrderHub.Domain.Enums;
using System;

namespace OrderHub.Domain.Models;

public class OutboxMessage : ModelBase
{
    public string Text { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; }

    public RecipientType RecipientType { get; set; }
    public MessageStatus Status { get; set; }

    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string ErrorMessage { get; set; }
}
