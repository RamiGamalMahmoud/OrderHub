using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;

namespace OrderHub.Domain.Models;

public class OutboxMessage : ModelBase
{
    public string Text { get; set; }
    public int? OrderId { get; set; }
    public Order Order { get; set; }

    public RecipientType RecipientType { get; set; }
    public OutboxMessageStatus Status { get; set; }

    public int? RecipientId { get; set; }
    public OutboxMessageRecipient Recipient { get; set; }

    public int? RetryCount { get; set; }
    public int? MaxRetries { get; set; } = 3;
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? SentAt { get; set; }

    public List<string> Notes { get; set; } = [];
    public List<string> LegacyAttachments { get; set; } = [];
    public List<OutboxMessageAttachment> Attachments { get; set; } = [];
}
